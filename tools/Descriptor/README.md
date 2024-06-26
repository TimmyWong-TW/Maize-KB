# Descriptor

Generate an overview for a sermon, speech, or long text.
The outline from the output may then be used for semantic chunking.

Include API key for Google Generative AI in `tools/secrets.env` as `GOOGLE_AI_API_KEY` when using with Docker Compose.

```sh
cd Maize-KB/tools
docker compose run --rm -d descriptor
```

## Exceptions

Some content could not pass the moderation of Google AI, while some output could exceed token budget.

Refer to `overview/error.log` for files that require manual actions. In case Gemini 1.5 Pro also refuses to generate useful output, `gpt-4-turbo-2024-04-09` or another large language model may be used manually.