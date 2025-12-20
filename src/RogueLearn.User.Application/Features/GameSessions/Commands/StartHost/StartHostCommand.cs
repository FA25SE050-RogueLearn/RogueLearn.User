using MediatR;
using System.Diagnostics;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.StartHost
{
    public record StartHostCommand(string? UserId) : IRequest<StartHostResult>;

    public record StartHostResult(bool Ok, string? JoinCode, string? HostId, string? Message, string? RawLog, string? WsUrl, string? Error);

    public class StartHostCommandHandler : IRequestHandler<StartHostCommand, StartHostResult>
    {
        private static readonly System.Text.RegularExpressions.Regex JoinCodeRegex =
            new(@"Relay\s+Join\s+Code\s*:\s*([A-Z0-9]{6,12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        public async Task<StartHostResult> Handle(StartHostCommand request, CancellationToken cancellationToken)
        {
            var image = Env("RL_DOCKER_IMAGE", "roguelearn-server:latest");
            var baseName = Env("RL_DOCKER_CONTAINER", string.Empty);
            var name = string.IsNullOrWhiteSpace(baseName)
                ? $"roguelearn-server-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                : $"{baseName}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var envs = new List<string>
            {
                $"UNITY_SERVER_SCENE={Env("UNITY_SERVER_SCENE", "HostUI")}",
                $"RELAY_REGION={Env("RELAY_REGION", "us-central")}",
                $"RL_MAX_CONNECTIONS={Env("RL_MAX_CONNECTIONS", "20")}"
            };

            var userApiBase = Env("USER_API_BASE", Env("RL_DOCKER_USER_API_BASE", string.Empty));
            if (!string.IsNullOrWhiteSpace(userApiBase))
            {
                envs.Add($"USER_API_BASE={userApiBase.TrimEnd('/')}");
            }
            envs.Add($"INSECURE_TLS={Env("INSECURE_TLS", "0")}");

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                envs.Add($"USER_ID={request.UserId}");
            }

            var runArgs = new List<string> { "run", "--rm", "--name", name, "-d" };
            var portHost = Env("RL_DOCKER_PORT_HOST", string.Empty);
            var portContainer = Env("RL_DOCKER_PORT_CONTAINER", "8080");
            var portMappingAdded = false;
            if (!string.IsNullOrWhiteSpace(portHost))
            {
                runArgs.AddRange(new[] { "-p", $"{portHost}:{portContainer}" });
                portMappingAdded = true;
            }

            AddIfSet(runArgs, "--cpus", Env("RL_DOCKER_CPUS", string.Empty));
            AddIfSet(runArgs, "--cpuset-cpus", Env("RL_DOCKER_CPUSET", string.Empty));
            AddIfSet(runArgs, "-m", Env("RL_DOCKER_MEMORY", string.Empty));
            AddIfSet(runArgs, "--cpu-shares", Env("RL_DOCKER_CPU_SHARES", string.Empty));

            var extraArgs = Env("RL_DOCKER_EXTRA_ARGS", string.Empty);
            if (!string.IsNullOrWhiteSpace(extraArgs))
            {
                runArgs.AddRange(extraArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var envPort = Env("RL_DOCKER_ENV_PORT", portContainer);
            if (!string.IsNullOrWhiteSpace(envPort))
            {
                envs.Add($"PORT={envPort}");
            }

            var extraEnvs = Env("RL_DOCKER_EXTRA_ENVS", string.Empty);
            if (!string.IsNullOrWhiteSpace(extraEnvs))
            {
                envs.AddRange(extraEnvs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            foreach (var e in envs)
            {
                runArgs.AddRange(new[] { "-e", e });
            }

            if (OperatingSystem.IsLinux())
            {
                runArgs.Add("--add-host=host.docker.internal:host-gateway");
            }

            runArgs.Add(image!);

            try
            {
                var runResult = await RunProcessAsync("docker", runArgs, TimeSpan.FromSeconds(30), cancellationToken);
                if (runResult.exitCode != 0)
                {
                    if (portMappingAdded && LooksLikePortBindingFailure(runResult.stderr))
                    {
                        var retryArgs = RemovePortMappingArgs(runArgs);
                        runResult = await RunProcessAsync("docker", retryArgs, TimeSpan.FromSeconds(30), cancellationToken);
                    }

                    if (runResult.exitCode != 0)
                    {
                        return new StartHostResult(false, null, null, null, null, null, $"docker run failed: {runResult.stderr}");
                    }
                }

                var timeoutMs = int.TryParse(Env("RL_LOG_TIMEOUT_MS", "20000"), out var ms) ? ms : 20000;
                var joinData = await ReadJoinCodeFromLogs(name, TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);

                if (string.IsNullOrWhiteSpace(joinData.joinCode))
                {
                    await ForceRemoveContainer(name);
                    return new StartHostResult(false, null, null, null, null, null, "Failed to obtain join code from container logs");
                }

                return new StartHostResult(
                    true,
                    joinData.joinCode,
                    name,
                    $"Unity headless server started in Docker ({image}).",
                    joinData.rawLine,
                    Env("NEXT_PUBLIC_GAME_WS_URL", null),
                    null);
            }
            catch (OperationCanceledException)
            {
                await ForceRemoveContainer(name);
                return new StartHostResult(false, null, null, null, null, null, "Timed out starting host");
            }
            catch (Exception ex)
            {
                await ForceRemoveContainer(name);
                return new StartHostResult(false, null, null, null, null, null, ex.Message);
            }
        }

        private static bool LooksLikePortBindingFailure(string stderr)
        {
            if (string.IsNullOrWhiteSpace(stderr)) return false;
            return stderr.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase)
                   || stderr.Contains("bind:", StringComparison.OrdinalIgnoreCase)
                   || stderr.Contains("address already in use", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> RemovePortMappingArgs(List<string> runArgs)
        {
            var copy = new List<string>(runArgs);
            for (var i = 0; i < copy.Count - 1; i++)
            {
                if (copy[i] == "-p")
                {
                    copy.RemoveAt(i);
                    copy.RemoveAt(i);
                    break;
                }
            }
            return copy;
        }

        private static string? Env(string key, string? defaultValue) =>
            Environment.GetEnvironmentVariable(key) ?? defaultValue;

        private static void AddIfSet(List<string> args, string flag, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                args.AddRange(new[] { flag, value });
            }
        }

        private static async Task<(string? joinCode, string? rawLine)> ReadJoinCodeFromLogs(
            string containerName,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var tcs = new TaskCompletionSource<(string?, string?)>(TaskCreationOptions.RunContinuationsAsynchronously);

            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs -f {containerName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                TryParseLine(e.Data);
            };
            proc.ErrorDataReceived += (_, _) => { };
            proc.Exited += (_, __) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetException(new InvalidOperationException("docker logs exited before join code was found"));
                }
            };

            void TryParseLine(string line)
            {
                try
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("{"))
                    {
                        using var doc = JsonDocument.Parse(trimmed);
                        if (doc.RootElement.TryGetProperty("event", out var ev) &&
                            ev.GetString()?.Equals("relay_join_code", StringComparison.OrdinalIgnoreCase) == true &&
                            doc.RootElement.TryGetProperty("joinCode", out var jc))
                        {
                            tcs.TrySetResult((jc.GetString(), line));
                            return;
                        }
                    }

                    var m = JoinCodeRegex.Match(line);
                    if (m.Success)
                    {
                        tcs.TrySetResult((m.Groups[1].Value, line));
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            try
            {
                var result = await tcs.Task.WaitAsync(timeout, cts.Token);
                return result;
            }
            finally
            {
                if (!proc.HasExited)
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                }
            }
        }

        private static async Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(
            string fileName,
            IEnumerable<string> args,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            string cwd = Environment.CurrentDirectory;

            if (!OperatingSystem.IsWindows())
            {
                cwd = "/";
            }

            if (fileName.Equals("docker", StringComparison.OrdinalIgnoreCase) && File.Exists("/usr/bin/docker"))
            {
                fileName = "/usr/bin/docker";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = cwd
            };

            using var proc = new Process { StartInfo = psi };
            var stdout = new List<string>();
            var stderr = new List<string>();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.Add(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.Add(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            await proc.WaitForExitAsync(cts.Token);

            return (proc.ExitCode, string.Join("\n", stdout), string.Join("\n", stderr));
        }

        private static async Task ForceRemoveContainer(string name)
        {
            try
            {
                await RunProcessAsync("docker", new[] { "rm", "-f", name }, TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch
            {
                // best effort
            }
        }
    }
}
