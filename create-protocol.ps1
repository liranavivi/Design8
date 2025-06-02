# Create a protocol for BaseProcessor testing
$protocol = @{
    id = "550e8400-e29b-41d4-a716-446655440000"
    name = "BaseProcessorProtocol"
    version = "1.0"
    description = "Protocol for BaseProcessor testing and validation"
    schema = @{
        type = "object"
        properties = @{
            data = @{
                type = "object"
                properties = @{
                    entities = @{ type = "array"; items = @{ type = "object" } }
                    parameters = @{ type = "object" }
                    context = @{ type = "object" }
                }
            }
            metadata = @{
                type = "object"
                properties = @{
                    source = @{ type = "string" }
                    timestamp = @{ type = "string"; format = "date-time" }
                    version = @{ type = "string" }
                }
            }
        }
        required = @("data")
    }
}

$json = $protocol | ConvertTo-Json -Depth 10

try {
    Write-Host "Creating protocol with ID: $($protocol.id)"
    $response = Invoke-RestMethod -Uri 'http://localhost:5130/api/protocols' -Method POST -Body $json -ContentType 'application/json'
    Write-Host "Successfully created protocol:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error creating protocol: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}
