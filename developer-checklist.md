# Developer Endpoint & Application Layer Checklist

Use this checklist when adding or modifying API endpoints and their corresponding feature handlers. It encodes the project rules for endpoints, mapping, and the Application layer boundaries.

## Per Endpoint

- [ ] Comment the endpoint with a clear summary of what it does (purpose, inputs, outputs, side-effects). Prefer XML doc comments or Swagger annotations so it appears in API docs.
- [ ] If admin-authenticated only:
  - [ ] Apply the `[AdminOnly]` attribute
  - [ ] Prefix the route with `/admin` (e.g., `api/admin/...`)
- [ ] If the endpoint needs the authenticated user id, use `ClaimsPrincipalExtensions.GetAuthUserId(User)` (do not parse claims manually).
- [ ] Define explicit response types for all outcomes (success and expected errors):
  - [ ] Use `[ProducesResponseType(typeof(MyResponseDto), StatusCodes.Status200OK)]` (and other status codes as needed)
  - [ ] Return strongly-typed DTOs and keep response shape consistent
- [ ] Keep endpoint thin: no business logic in the endpoint. Delegate to the corresponding feature handler.
- [ ] Use mapping profiles for input/output conversions:
  - [ ] Map request DTOs to commands/queries
  - [ ] Map domain/entities to response DTOs
- [ ] Apply consistent error handling (e.g., ProblemDetails) and logging per standards.
- [ ] Ensure naming and routing conventions are consistent with the rest of the API.

## Feature Handlers (Business Logic Lives Here)

- [ ] Implement all business and orchestration logic in the feature handler corresponding to the endpoint.
- [ ] Accept well-defined inputs (command/query DTO) and return a well-defined result/DTO.
- [ ] Use `CancellationToken` and propagate it to downstream calls.
- [ ] Validate inputs (and any invariants) within the handler.
- [ ] Use mapping profiles for conversions to/from domain models as needed.
- [ ] Keep handlers cohesive and unit-testable.
- [ ] Use FluentValidation consistently:
  - [ ] Prefer validators wired via the pipeline (`ValidationBehaviour`) for request DTOs.
  - [ ] When the handler builds domain data internally (e.g., parsed text), validate it via `IValidator<T>` and surface errors using `ValidationException` or the response contract.
- [ ] Throw standardized exceptions for expected errors so middleware can translate them:
  - [ ] `NotFoundException` for missing resources
  - [ ] `ValidationException` for invalid inputs
  - [ ] Avoid returning `null` on failure paths; rely on centralized error handling.
- [ ] Do not call 3rd-party services directly in handlers:
  - [ ] Interact with external providers via interfaces defined in `Application/Interfaces` and implemented in `Infrastructure`.
- [ ] Ensure idempotency and determinism where applicable:
  - [ ] Check for existing records/relationships before creating to avoid duplicates.
  - [ ] Perform all validations before side-effects (storage, messaging).
- [ ] Logging standards:
  - [ ] Use structured logging with key identifiers; include "starting" and "completed" messages for major operations.
  - [ ] Do not log sensitive data (PII) or large payloads.
- [ ] Authorization and user context:
  - [ ] Do not parse claims inside handlers; receive `AuthUserId` in the command/query.
  - [ ] If richer context is needed, use `IUserContextService` to build it.
- [ ] Consistency and persistence:
  - [ ] Group related writes; avoid partial updates.
  - [ ] Set `CreatedAt`/`UpdatedAt` and other audit fields in handlers (not in mappers).
- [ ] Performance & scalability:
  - [ ] Avoid N+1 repository calls; prefer batching and pagination.
  - [ ] Minimize unnecessary data loading; map only what the response requires.
- [ ] Propagate cancellation & resilience:
  - [ ] Pass `CancellationToken` to repositories/services.
  - [ ] Apply retry/timeout policies in `Infrastructure` for external calls (via abstractions).

## Mapping Profiles (Mapper)

- [ ] Add/update AutoMapper profiles (or equivalent) for all new DTOs and responses.
- [ ] Centralize mapping rules in profiles; avoid ad-hoc mapping in endpoints/handlers.
- [ ] Handle nulls, collections, and nested objects explicitly in mappings.
- [ ] Validate mappings (unit tests or runtime assertions where appropriate).
- [ ] Keep DTOs and domain models decoupled.
- [ ] Location & organization:
  - [ ] Define mappings in `Application/Mappings/MappingProfile.cs` (or feature-specific profiles if they remain small and cohesive).
- [ ] Non-trivial conversions:
  - [ ] Prefer `ForMember(...).MapFrom(...)` for computed fields.
  - [ ] Use `AfterMap` only for pure, side-effect-free transformations (e.g., JSON serialization), as seen in `Achievement` mappings.
- [ ] Avoid side-effects in mapping:
  - [ ] Do not call repositories/services in profiles or resolvers; perform enrichment in handlers.
- [ ] Explicit ignores:
  - [ ] `ForMember(..., opt => opt.Ignore())` for properties that are set manually in handlers (e.g., `UserRoleDto.RoleName`).
- [ ] Collections & nulls:
  - [ ] Ensure element mappings exist for collections and be explicit about null vs empty lists.
- [ ] Directional mapping:
  - [ ] Provide reverse maps only when needed; avoid overwriting domain invariants.
- [ ] IDs & audit fields:
  - [ ] Do not map `Id`, `CreatedAt`, `UpdatedAt` from request DTOs to domain entities; set them in handlers.
- [ ] Performance:
  - [ ] Map only required fields; avoid mapping entire graphs when the response needs a subset.
- [ ] Documentation:
  - [ ] Comment any mapping that deviates from straightforward property-to-property mapping to explain intent.

## Application Layer: Services & Interfaces

- [ ] Restrict `Services` and `Interfaces` (in the Application layer) to 3rd-party integrations only (e.g., Supabase Storage, AI services, external providers).
- [ ] Define interfaces in `Application` for external services; implement them in `Infrastructure`.
- [ ] Do NOT place domain/business services here; domain logic stays in feature handlers/domain layer.
- [ ] Encapsulate provider-specific concerns behind clean abstractions.
- [ ] Apply resilience (retry, timeout, circuit breaker) where appropriate for external calls.

## Application Layer: Models

- [ ] Only place models in `Application/Models` that represent AI-related data or any 3rd-party service payloads.
- [ ] Do NOT store domain entities or core domain models here.
- [ ] Document model purpose and originating service.

## Final Checks Before Review

- [ ] Endpoint comments present and clear.
- [ ] Admin-only rules applied and `/admin` route prefix used where required.
- [ ] `GetAuthUserId` used wherever user id is needed.
- [ ] Explicit response types declared for all outcomes.
- [ ] Endpoint is thin; business logic is in the handler.
- [ ] Mapping profiles are used for all DTO/domain conversions.
- [ ] Application layer boundaries respected for services, interfaces, and models.