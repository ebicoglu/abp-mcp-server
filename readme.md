# ABP MCP Server Walkthrough

I have created the ABP MCP Server as a C# Console Application. It implements the Model Context Protocol to provide ABP Framework related tools to AI agents.

## Features Implemented

1. ABP Documentation Search

    

   (

   ```
   abp.docs.search
   ```

   )

   - Scrapes `abp.io/docs` to find relevant documentation pages.

2. GitHub Issues Search

    

   (

   ```
   abp.github.issues.search
   ```

   )

   - Uses GitHub REST API to search `abpframework/abp` repository issues.

3. Support Questions Search

    

   (

   ```
   abp.support.questions.search
   ```

   )

   - Scrapes `abp.io/support/questions` for community questions.

4. Community Articles Search

    

   (

   ```
   abp.community.articles.search
   ```

   )

   - Scrapes `abp.io/community/articles` for articles.

## Project Structure

- `AbpMcpServer.csproj`: Main project file (.NET 8/9).

- Program.cs

  : Entry point, dependency injection setup.

- Services/McpServer.cs

  : Handles JSON-RPC over STDIO.

- Services/McpRouter.cs

  : Routes requests to tools.

- `Tools/*.cs`: Implementation of specific tools.

- Models/JsonRpc.cs

  : JSON-RPC data models.

## How to Run

1. Build

   :

   ```
   dotnet build
   ```

2. Run

   :

   ```
   dotnet run
   ```

   (Note: It expects JSON-RPC input via Stdin)

## AntiGravity Integration

To use this server with AntiGravity or Cursor, you need to configure it in the MCP settings.

### Configuration

```
{

  "mcpServers": {

    "abp-helper": {

      "command": "dotnet",

      "args": [

        "run",

        "--project",

        "d:/github/alper/abp-mcp-server/AbpMcpServer.csproj"

      ]

    }

  }

}
```

### Prompt Templates

Here are the prompt templates you requested to make the best use of this server.

#### 1. Local Reasoning Model Prompt

```
SYSTEM:

You are an expert ABP Framework development assistant.

You ONLY answer based on information retrieved from the MCP tools.

Do not hallucinate APIs or features.



USER:

<<User Question>>



TOOLS:

- mcp.abp.docs.search

- mcp.abp.github.issues.search

- mcp.abp.support.questions.search

- mcp.abp.community.articles.search



PLAN:

1) Search docs using user query to find official guidance.

2) Search GitHub matching issues to see if it's a known bug or feature request.

3) Search support if unresolved to find community solutions.

4) Search community articles for tutorials or deep dives.

5) Generate concise actionable fix based on findings.

6) Provide short code sample if applicable.
```

#### 2. Fix Code Prompt

```
@ABP Ask MCP

- Diagnose the following code:

  ```csharp

  <<Selected Code>>
```

- Find documentation reference for the used classes.
- Find similar GitHub issues if there are potential bugs.
- Propose a fix or improvement.
- Produce example code.

```
#### 3. Architecture Question Prompt



```markdown

Compare ABP documentation and community articles regarding:

<<Topic>>



Return a short decision summary on the best approach.
```

#### 4. Error Resolution Prompt

```
Resolve the following error:

<<Error Message>>



Using:

- abp.docs.search

- abp.github.issues.search

- abp.support.questions.search
```