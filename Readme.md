# CosmosSQLcmd

## Azure Cosmos DB SQL Command Line Client Tool

This utility is a lightweight Command-Line Client for Aure Cosmos DB. It has a versatile editor for editing and executing queries directly from a Console Window! 

## Usage

```
CosmosSQLcmd --endpoint yourcosmosdbURI --key yourAccesskey --database yourDB --container yourContainer [--cp direct|gateway] [--maxfetchsize 10] [--Metrics]

  --endpoint        Required. Azure Cosmos DB account endpoint URI
  --key             Required. Azure Cosmos DB account read access key
  --database        Required. Target database to use
  --container       Required. Target container to use
  --cp              Connection policy:Direct|Gateway (Default:Direct)
  --maxfetchsize    Number of items per fetch (Default:100)
  --metrics         Include metrics
  --help            Display this help screen.
```

## External Nuget Dependencies:

    Include="CommandLineParser" Version="2.8.0" 
    Include="Microsoft.Azure.Cosmos" Version="3.23.0"
    Include="Newtonsoft.Json" Version="13.0.1"

## Example:

```
CosmosSQLcmd | (https://cosmosdb.documents.azure.com:443/gateway--maxfetchsize)(db)(data)
Editor Mode | Press CTRL+E to execute query | ESC to exit

SELECT count(c)
FROM c

...Fetching (max:100)...

------------------------------------------------------------------------------------
{
  "_rid": "t6UpAM7zVcA=",
  "Documents": [
    {
      "$1": 15375
    }
  ],
  "_count": 1
}
------------------------------------------------------------------------------------


Query Completed. Press any key to return to the editor
```

# Screenshots

## Editor Mode
![Editor Screenshot](\Screenshots\ScreenshotEditorMode.png "Editor Mode Screenshot")

## Query Results
![Query Results Screenshot](\Screenshots\ScreenshotResults.png "Query Results Screenshot")

## Screenshot
![Screenshot](\Screenshots\Screenshot.png "Screenshot")
