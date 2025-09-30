// Jenkinsfile (Corrected with Kaniko and deleteDir)

pipeline {
    // We only need a Kaniko agent now for the build stage.
    // Other stages will use a minimal agent.
    agent any

    // --- Environment variables are unchanged ---
    environment {
        HARBOR_URL           = "harbor.roguelearn.site" // Your Harbor domain
        HARBOR_PROJECT       = "roguelearn"
        APP_IMAGE_NAME       = "roguelearn-user-api"
        HARBOR_IMAGE_PATH    = "${HARBOR_URL}/${HARBOR_PROJECT}/${APP_IMAGE_NAME}"
        KUSTOMIZE_BASE_IMAGE = "soybean2610/roguelearn-user-api"
    }

    stages {
        stage('Build and Test') {
            // This stage needs the .NET SDK
            agent {
                kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:9.0
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('dotnet') {
                    sh 'dotnet restore'
                    sh 'dotnet build --configuration Release --no-restore'
                    sh 'dotnet test --configuration Release --no-build --verbosity normal'
                }
            }
        }

        // --- REPLACED Build and Push stages with a single Kaniko stage ---
        stage('Build and Push to Harbor with Kaniko') {
            // This stage only runs on the main branch
            when { branch 'main' }
            // This stage needs a special Kaniko agent
            agent {
                kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: kaniko
    image: gcr.io/kaniko-project/executor:v1.10.0-debug # Use a specific, stable version
    imagePullPolicy: Always
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('kaniko') {
                    // Kaniko authenticates using a Docker config.json file.
                    // We use withCredentials to create this file securely in the container's home directory.
                    withCredentials([usernamePassword(credentialsId: 'harbor-credentials', usernameVariable: 'HARBOR_USER', passwordVariable: 'HARBOR_PASS')]) {
                        // Create the config file required by Kaniko
                        sh """
                        echo "{\\"auths\\":{\\"${HARBOR_URL}\\":{\\"username\\":\\"${HARBOR_USER}\\",\\"password\\":\\"${HARBOR_PASS}\\",\\"auth\\":\\"$(echo -n ${HARBOR_USER}:${HARBOR_PASS} | base64)\\"}}}" > /kaniko/.docker/config.json
                        """

                        // Execute Kaniko. It builds AND pushes in one command.
                        sh """
                        /kaniko/executor --context=\`pwd\` \
                                         --dockerfile=src/RogueLearn.User.Api/Dockerfile \
                                         --destination=${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)} \
                                         --destination=${HARBOR_IMAGE_PATH}:latest
                        """
                    }
                }
            }
        }

        stage('Update K8s Manifests') {
            when { branch 'main' }
            // This stage needs git and kustomize
            agent {
                 kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: kustomize
    image: alpine/k8s:1.28.4
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('kustomize') {
                    withCredentials([string(credentialsId: 'github-token', variable: 'GIT_TOKEN')]) {
                        sh """
                        git clone https://oauth2:${GIT_TOKEN}@github.com/FA25SE050-RogueLearn/RogueLearn.Kubernetes.git k8s-manifests
                        cd k8s-manifests
                        git config user.name "Jenkins CI"
                        git config user.email "jenkins@your-domain.com"
                        cd roguelearn-user-api/base
                        
                        # This kustomize command is unchanged
                        kustomize edit set image ${KUSTOMIZE_BASE_IMAGE}=${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)}
                        
                        cd ../..
                        git add .
                        if ! git diff --staged --quiet; then
                          git commit -m "ci: Update image for roguelearn-user-api to ${env.GIT_COMMIT.substring(0,8)} (from Harbor)"
                          git push
                        else
                          echo "No changes to commit"
                        fi
                        """
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline finished.'
            // --- FIXED: Replaced cleanWs() with the built-in deleteDir() ---
            deleteDir() // Clean up the workspace
        }
    }
}