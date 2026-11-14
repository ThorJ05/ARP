# bun-react-template

To install dependencies:

```bash
bun install
```

To start a development server:

```bash
bun dev
```

To run for production:

```bash
bun start
```

This project was created using `bun init` in bun v1.3.14. [Bun](https://bun.com) is a fast all-in-one JavaScript runtime.

## API client

The TypeScript client in `src/api/api.ts` is generated from the backend's OpenAPI
specification with [swagger-typescript-api](https://github.com/acacode/swagger-typescript-api),
so the frontend and backend cannot drift apart. Do not edit the file by hand.

To regenerate it:

1. Start the database: `docker compose up -d`
2. Start the API (profile `http`, listens on `http://localhost:5288`)
3. Run, from the repository root:

```bash
bun run generate:api
```

Commit the updated `src/api/api.ts` in the same pull request as the backend change
(new or changed endpoints and DTOs).
