 = Get-Content changelog_snapshot.json -Raw -Encoding UTF8;  = ConvertFrom-Json ; Write-Host (" schema=\ + ._schema + " entries=\ + ._entries.Count)
