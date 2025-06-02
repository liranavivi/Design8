# Schema-Based Processor Configuration Test

## Overview
This document outlines the testing approach for the new schema-based processor configuration system.

## Test Scenarios

### 1. Processor Initialization with Schema Retrieval
**Objective**: Verify that processors automatically retrieve schema definitions during initialization.

**Steps**:
1. Start EntitiesManager API
2. Create test schemas with known IDs
3. Configure BaseProcessor with schema IDs
4. Start BaseProcessor application
5. Verify schema definitions are retrieved and stored

**Expected Results**:
- Processor initializes successfully
- Schema definitions are retrieved via MassTransit
- InputSchemaDefinition and OutputSchemaDefinition are populated
- Validation uses retrieved schema definitions

### 2. Schema Validation with Retrieved Definitions
**Objective**: Verify that schema validation works with dynamically retrieved definitions.

**Steps**:
1. Configure processor with valid schema IDs
2. Send activity execution command with valid input data
3. Send activity execution command with invalid input data
4. Verify validation results

**Expected Results**:
- Valid data passes validation
- Invalid data fails validation with proper error messages
- Validation uses retrieved schema definitions, not hardcoded schemas

### 3. Missing Schema Handling
**Objective**: Verify graceful handling of missing schema definitions.

**Steps**:
1. Configure processor with non-existent schema IDs
2. Start processor
3. Attempt to process activities

**Expected Results**:
- Processor starts successfully despite missing schemas
- Warning logs are generated for missing schemas
- Validation is skipped with appropriate logging
- System continues to operate

### 4. Configuration Migration Verification
**Objective**: Verify that ProtocolId has been completely removed.

**Steps**:
1. Review ProcessorConfiguration class
2. Review appsettings.json
3. Review ProcessorService implementation
4. Verify no ProtocolId references remain

**Expected Results**:
- No ProtocolId properties in ProcessorConfiguration
- No ProtocolId in configuration files
- No ProtocolId usage in service implementations
- Schema-based configuration is fully functional

## Test Commands

### Create Test Schemas
```bash
# Create input schema
curl -X POST "http://localhost:5000/api/schemas" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.0",
    "name": "TestInputSchema",
    "description": "Test input schema for processor",
    "definition": "{\"type\":\"object\",\"properties\":{\"data\":{\"type\":\"object\"},\"metadata\":{\"type\":\"object\"}},\"required\":[\"data\"]}"
  }'

# Create output schema
curl -X POST "http://localhost:5000/api/schemas" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.0", 
    "name": "TestOutputSchema",
    "description": "Test output schema for processor",
    "definition": "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"},\"timestamp\":{\"type\":\"string\"},\"status\":{\"type\":\"string\"}},\"required\":[\"result\",\"status\"]}"
  }'
```

### Update Processor Configuration
Update `appsettings.json` with the returned schema IDs:
```json
{
  "ProcessorConfiguration": {
    "Version": "1.0",
    "Name": "TestProcessor",
    "Description": "Test processor with schema-based configuration",
    "InputSchemaId": "RETURNED_INPUT_SCHEMA_ID",
    "OutputSchemaId": "RETURNED_OUTPUT_SCHEMA_ID"
  }
}
```

### Start and Test Processor
```bash
# Start the processor
dotnet run --project src/Framework/FlowOrchestrator.BaseProcessor.Application

# Monitor logs for schema retrieval messages
# Look for: "Retrieving schema definitions for InputSchemaId: ..., OutputSchemaId: ..."
# Look for: "Successfully retrieved input/output schema definition"
```

## Success Criteria

✅ **Schema Retrieval**: Processor automatically retrieves schema definitions during initialization
✅ **Dynamic Validation**: Schema validation uses retrieved definitions instead of hardcoded schemas  
✅ **Error Handling**: System gracefully handles missing schemas without crashing
✅ **Configuration Migration**: ProtocolId completely removed from all components
✅ **Backward Compatibility**: Existing functionality continues to work with new schema-based approach
✅ **Build Success**: All projects compile successfully
✅ **Documentation**: README and configuration examples updated

## Migration Benefits

1. **Centralized Schema Management**: Schemas are managed centrally in the SchemasManager
2. **Dynamic Configuration**: Schema definitions can be updated without redeploying processors
3. **Consistency**: All processors use the same schema management system
4. **Flexibility**: Easier to manage multiple schema versions and variations
5. **Maintainability**: Reduced duplication of schema definitions across configurations
