# Spec: trace-coverage

## Objective
The two remaining blind spots in traces: SignalR hub traffic and OpenAI extraction calls
(currently a bare `POST v1/responses` HTTP span with no model, token or outcome information).

## Design
**SignalR** — ServiceDefaults `tracing.AddSource("Microsoft.AspNetCore.SignalR.Server")`
(built-in ActivitySource in ASP.NET Core 9+; no package).

**OpenAI** — new `src/Infrastructure/AI/GenAiTelemetry.cs`: ActivitySource + Meter
`skestock.AI`. `OpenAiDocumentExtractionClient.SendAsync` wraps the call in a Client span following
GenAI semantic conventions:
- name `chat {model}`; `gen_ai.operation.name=chat`, `gen_ai.provider.name=openai`,
  `gen_ai.request.model`, and from the response `gen_ai.response.model`, `gen_ai.response.id`,
  `gen_ai.usage.input_tokens`, `gen_ai.usage.output_tokens` (from `usage.input_tokens` /
  `usage.output_tokens`), `skestock.extraction.result_type=<TResult name>`.
- Failures: status Error + `error.type` (`transient`, `unprocessable`, HTTP status code, or exception
  type).
- Metrics: `gen_ai.client.token.usage` histogram `{token}` tagged `gen_ai.token.type`
  (`input`/`output`) + model; `gen_ai.client.operation.duration` histogram `s` tagged model and
  `error.type` on failure.
- **Never** record prompts, schema, file names, file data or output text.
- The existing HttpClient span becomes a child of this span automatically.

## Success criteria
1. Unit test (fake `HttpMessageHandler`, existing test style in
   `tests/Application.UnitTests/Documents`): success emits one `skestock.AI` Client span with model +
   token tags and records both histograms; a 500 emits Error status with `error.type=500` and the
   existing `TransientExtractionException` is still thrown.
2. Test asserts no tag value contains the prompt text or base64 file data.
3. ServiceDefaults registers the SignalR source (asserted by inspecting a built `TracerProvider`
   via an `ActivityListener` smoke test or code review — lightweight).
4. Build clean; all Application unit tests pass.
