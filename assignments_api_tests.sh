#!/bin/bash

# Comprehensive AssignmentsController API Tests
# Testing all endpoints and their possible status codes

BASE_URL="http://localhost:5130/api/assignments"
STEPS_URL="http://localhost:5130/api/steps"
ADDRESSES_URL="http://localhost:5130/api/addresses"
SCHEMAS_URL="http://localhost:5130/api/schemas"

echo "=========================================="
echo "COMPREHENSIVE ASSIGNMENTS CONTROLLER API TESTS"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print test header
print_test() {
    echo -e "${BLUE}=========================================="
    echo -e "TEST: $1"
    echo -e "==========================================${NC}"
}

# Function to print status code result
print_result() {
    local expected=$1
    local actual=$2
    local description=$3
    
    if [ "$expected" = "$actual" ]; then
        echo -e "${GREEN}✅ PASS${NC} - Expected: $expected, Got: $actual - $description"
    else
        echo -e "${RED}❌ FAIL${NC} - Expected: $expected, Got: $actual - $description"
    fi
    echo ""
}

# Function to extract value from JSON response (simple grep-based approach)
extract_json_value() {
    local json="$1"
    local key="$2"
    echo "$json" | grep -o "\"$key\":\"[^\"]*\"" | cut -d'"' -f4
}

# Setup: Create test entities for foreign key validation
echo "=========================================="
echo "SETUP: Creating test entities for validation"
echo "=========================================="

# 1. Create a test schema
SCHEMA_JSON='{
  "version": "1.0.0",
  "name": "TestSchemaForAssignments",
  "description": "Test schema for assignment validation",
  "definition": "{\"type\": \"object\", \"properties\": {\"test\": {\"type\": \"string\"}}}"
}'

echo "Creating test schema..."
SCHEMA_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$SCHEMA_JSON" \
  -o /tmp/schema_response.json \
  "$SCHEMAS_URL")

if [ "$SCHEMA_RESPONSE" = "201" ]; then
    SCHEMA_ID=$(cat /tmp/schema_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test schema created with ID: $SCHEMA_ID"
elif [ "$SCHEMA_RESPONSE" = "409" ]; then
    echo "⚠️ Test schema already exists, getting existing schema..."
    curl -s "$SCHEMAS_URL" -o /tmp/existing_schemas.json
    SCHEMA_ID=$(cat /tmp/existing_schemas.json | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing schema ID: $SCHEMA_ID"
else
    echo "❌ Failed to create test schema (status: $SCHEMA_RESPONSE)"
    exit 1
fi

# 2. Create a test processor (for StepEntity.EntityId validation)
PROCESSORS_URL="http://localhost:5130/api/processors"
PROCESSOR_JSON='{
  "version": "1.0.0",
  "name": "TestProcessorForAssignments",
  "description": "Test processor for assignment validation",
  "protocolId": "'$SCHEMA_ID'",
  "inputSchemaId": "'$SCHEMA_ID'",
  "outputSchemaId": "'$SCHEMA_ID'"
}'

echo "Creating test processor..."
PROCESSOR_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$PROCESSOR_JSON" \
  -o /tmp/processor_response.json \
  "$PROCESSORS_URL")

if [ "$PROCESSOR_RESPONSE" = "201" ]; then
    PROCESSOR_ID=$(cat /tmp/processor_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test processor created with ID: $PROCESSOR_ID"
elif [ "$PROCESSOR_RESPONSE" = "409" ]; then
    echo "⚠️ Test processor already exists, getting existing processor..."
    curl -s "$PROCESSORS_URL" -o /tmp/existing_processors.json
    PROCESSOR_ID=$(cat /tmp/existing_processors.json | grep -A 10 -B 10 "TestProcessorForAssignments" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing processor ID: $PROCESSOR_ID"
else
    echo "❌ Failed to create test processor (status: $PROCESSOR_RESPONSE)"
    echo "Response:"
    cat /tmp/processor_response.json
    exit 1
fi

# 3. Create a test address (for EntityIds validation)
TIMESTAMP=$(date +%s)
ADDRESS_JSON='{
  "version": "1.0.0",
  "name": "TestAddressForAssignments",
  "description": "Test address for assignment validation",
  "address": "test-address-for-assignments-'$TIMESTAMP'",
  "schemaId": "'$SCHEMA_ID'"
}'

echo "Creating test address..."
ADDRESS_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$ADDRESS_JSON" \
  -o /tmp/address_response.json \
  "$ADDRESSES_URL")

if [ "$ADDRESS_RESPONSE" = "201" ]; then
    ADDRESS_ID=$(cat /tmp/address_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test address created with ID: $ADDRESS_ID"
elif [ "$ADDRESS_RESPONSE" = "409" ]; then
    echo "⚠️ Test address already exists, getting existing address..."
    curl -s "$ADDRESSES_URL" -o /tmp/existing_addresses.json
    ADDRESS_ID=$(cat /tmp/existing_addresses.json | grep -A 10 -B 10 "TestAddressForAssignments" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing address ID: $ADDRESS_ID"
else
    echo "❌ Failed to create test address (status: $ADDRESS_RESPONSE)"
    echo "Response:"
    cat /tmp/address_response.json
    exit 1
fi

# 4. Create a test step (for StepId validation)
STEP_JSON='{
  "version": "1.0.0",
  "name": "TestStepForAssignments",
  "description": "Test step for assignment validation",
  "entityId": "'$PROCESSOR_ID'",
  "nextStepIds": []
}'

echo "Creating test step..."
STEP_RESPONSE=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$STEP_JSON" \
  -o /tmp/step_response.json \
  "$STEPS_URL")

if [ "$STEP_RESPONSE" = "201" ]; then
    STEP_ID=$(cat /tmp/step_response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Test step created with ID: $STEP_ID"
elif [ "$STEP_RESPONSE" = "409" ]; then
    echo "⚠️ Test step already exists, getting existing step..."
    curl -s "$STEPS_URL" -o /tmp/existing_steps.json
    STEP_ID=$(cat /tmp/existing_steps.json | grep -A 10 -B 10 "TestStepForAssignments" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "✅ Using existing step ID: $STEP_ID"
else
    echo "❌ Failed to create test step (status: $STEP_RESPONSE)"
    echo "Response:"
    cat /tmp/step_response.json
    exit 1
fi

echo ""

# 1. GET /api/assignments - Get all assignments
print_test "1. GET /api/assignments - Get all assignments"

echo "Testing 200 OK - Successfully retrieve all assignments"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL")
print_result "200" "$response" "Get all assignments"
echo "Response body:"
cat /tmp/response.json
echo ""

# 2. GET /api/assignments/paged - Get paginated assignments
print_test "2. GET /api/assignments/paged - Get paginated assignments"

echo "Testing 200 OK - Valid pagination parameters"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=10")
print_result "200" "$response" "Valid pagination"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid page (0)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=0&pageSize=10")
print_result "400" "$response" "Invalid page parameter (0)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid pageSize (101)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/paged?page=1&pageSize=101")
print_result "400" "$response" "Invalid pageSize parameter (101)"
echo "Response body:"
cat /tmp/response.json
echo ""

# 3. GET /api/assignments/{id} - Get assignment by ID
print_test "3. GET /api/assignments/{id} - Get assignment by ID"

echo "Testing 404 Not Found - Non-existent ID"
NON_EXISTENT_ID="12345678-1234-1234-1234-123456789012"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent assignment ID"
echo "Response body:"
cat /tmp/response.json
echo ""

# 4. GET /api/assignments/by-key/{stepId} - Get assignment by composite key
print_test "4. GET /api/assignments/by-key/{stepId} - Get assignment by composite key"

echo "Testing 404 Not Found - Non-existent stepId"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent stepId"
echo "Response body:"
cat /tmp/response.json
echo ""

# 5. GET /api/assignments/by-step/{stepId} - Get assignment by step ID
print_test "5. GET /api/assignments/by-step/{stepId} - Get assignment by step ID"

echo "Testing 404 Not Found - Non-existent stepId"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-step/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent stepId"
echo "Response body:"
cat /tmp/response.json
echo ""

# 6. GET /api/assignments/by-entity/{entityId} - Get assignments by entity ID
print_test "6. GET /api/assignments/by-entity/{entityId} - Get assignments by entity ID"

echo "Testing 200 OK - Valid entityId (returns empty array if none found)"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-entity/$NON_EXISTENT_ID")
print_result "200" "$response" "Valid entityId parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

# 7. GET /api/assignments/by-name/{name} - Get assignments by name
print_test "7. GET /api/assignments/by-name/{name} - Get assignments by name"

echo "Testing 200 OK - Valid name parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-name/NonExistentName")
print_result "200" "$response" "Valid name parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Empty name parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-name")
print_result "400" "$response" "Empty name parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

# 8. GET /api/assignments/by-version/{version} - Get assignments by version
print_test "8. GET /api/assignments/by-version/{version} - Get assignments by version"

echo "Testing 200 OK - Valid version parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-version/1.0.0")
print_result "200" "$response" "Valid version parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Empty version parameter"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-version")
print_result "400" "$response" "Empty version parameter"
echo "Response body:"
cat /tmp/response.json
echo ""

# 9. POST /api/assignments - Create assignment
print_test "9. POST /api/assignments - Create assignment"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "null" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Null entity"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Invalid model state (missing required fields)"
INVALID_JSON='{
  "description": "Missing required fields"
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$INVALID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Invalid model state"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed (invalid StepId)"
INVALID_STEPID_JSON='{
  "version": "1.0.0",
  "name": "TestAssignmentInvalidStep",
  "description": "Test assignment with invalid step ID",
  "stepId": "00000000-0000-0000-0000-000000000000",
  "entityIds": ["'$ADDRESS_ID'"]
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$INVALID_STEPID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Foreign key validation failed (invalid StepId)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed (invalid EntityIds)"
INVALID_ENTITYIDS_JSON='{
  "version": "1.0.0",
  "name": "TestAssignmentInvalidEntity",
  "description": "Test assignment with invalid entity IDs",
  "stepId": "'$STEP_ID'",
  "entityIds": ["00000000-0000-0000-0000-000000000000"]
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$INVALID_ENTITYIDS_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "400" "$response" "Foreign key validation failed (invalid EntityIds)"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 201 Created - Valid assignment entity"
VALID_JSON='{
  "version": "1.0.0",
  "name": "TestAssignmentValid",
  "description": "Valid test assignment",
  "stepId": "'$STEP_ID'",
  "entityIds": ["'$ADDRESS_ID'"]
}'
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$VALID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "201" "$response" "Valid assignment entity"
echo "Response body:"
cat /tmp/response.json
echo ""

# Extract the created assignment ID for later tests
if [ "$response" = "201" ]; then
    ASSIGNMENT_ID=$(cat /tmp/response.json | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "✅ Created assignment ID: $ASSIGNMENT_ID"
fi

echo "Testing 409 Conflict - Duplicate composite key (same stepId)"
response=$(curl -s -w "%{http_code}" -X POST \
  -H "Content-Type: application/json" \
  -d "$VALID_JSON" \
  -o /tmp/response.json \
  "$BASE_URL")
print_result "409" "$response" "Duplicate composite key"
echo "Response body:"
cat /tmp/response.json
echo ""

# Test GET endpoints with created assignment
if [ ! -z "$ASSIGNMENT_ID" ]; then
    echo "Testing 200 OK - Get created assignment by ID"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/$ASSIGNMENT_ID")
    print_result "200" "$response" "Get created assignment by ID"
    echo "Response body:"
    cat /tmp/response.json
    echo ""

    echo "Testing 200 OK - Get assignment by composite key (stepId)"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-key/$STEP_ID")
    print_result "200" "$response" "Get assignment by composite key"
    echo "Response body:"
    cat /tmp/response.json
    echo ""

    echo "Testing 200 OK - Get assignment by step ID"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-step/$STEP_ID")
    print_result "200" "$response" "Get assignment by step ID"
    echo "Response body:"
    cat /tmp/response.json
    echo ""

    echo "Testing 200 OK - Get assignments by entity ID"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-entity/$ADDRESS_ID")
    print_result "200" "$response" "Get assignments by entity ID"
    echo "Response body:"
    cat /tmp/response.json
    echo ""

    echo "Testing 200 OK - Get assignments by name"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-name/TestAssignmentValid")
    print_result "200" "$response" "Get assignments by name"
    echo "Response body:"
    cat /tmp/response.json
    echo ""

    echo "Testing 200 OK - Get assignments by version"
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL/by-version/1.0.0")
    print_result "200" "$response" "Get assignments by version"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

# 10. PUT /api/assignments/{id} - Update assignment
print_test "10. PUT /api/assignments/{id} - Update assignment"

echo "Testing 400 Bad Request - Null entity"
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "null" \
  -o /tmp/response.json \
  "$BASE_URL/$NON_EXISTENT_ID")
print_result "400" "$response" "Null entity"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - ID mismatch"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    MISMATCH_JSON='{
      "id": "'$NON_EXISTENT_ID'",
      "version": "1.0.1",
      "name": "TestAssignmentMismatch",
      "description": "Test assignment with ID mismatch",
      "stepId": "'$STEP_ID'",
      "entityIds": ["'$ADDRESS_ID'"]
    }'
    response=$(curl -s -w "%{http_code}" -X PUT \
      -H "Content-Type: application/json" \
      -d "$MISMATCH_JSON" \
      -o /tmp/response.json \
      "$BASE_URL/$ASSIGNMENT_ID")
    print_result "400" "$response" "ID mismatch"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

echo "Testing 404 Not Found - Non-existent ID"
UPDATE_JSON='{
  "id": "'$NON_EXISTENT_ID'",
  "version": "1.0.1",
  "name": "TestAssignmentNotFound",
  "description": "Test assignment for non-existent ID",
  "stepId": "'$STEP_ID'",
  "entityIds": ["'$ADDRESS_ID'"]
}'
response=$(curl -s -w "%{http_code}" -X PUT \
  -H "Content-Type: application/json" \
  -d "$UPDATE_JSON" \
  -o /tmp/response.json \
  "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent ID"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 400 Bad Request - Foreign key validation failed (invalid StepId)"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    INVALID_UPDATE_JSON='{
      "id": "'$ASSIGNMENT_ID'",
      "version": "1.0.1",
      "name": "TestAssignmentInvalidUpdate",
      "description": "Test assignment with invalid step ID for update",
      "stepId": "00000000-0000-0000-0000-000000000000",
      "entityIds": ["'$ADDRESS_ID'"]
    }'
    response=$(curl -s -w "%{http_code}" -X PUT \
      -H "Content-Type: application/json" \
      -d "$INVALID_UPDATE_JSON" \
      -o /tmp/response.json \
      "$BASE_URL/$ASSIGNMENT_ID")
    print_result "400" "$response" "Foreign key validation failed (invalid StepId)"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

echo "Testing 200 OK - Valid update"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    VALID_UPDATE_JSON='{
      "id": "'$ASSIGNMENT_ID'",
      "version": "1.0.1",
      "name": "TestAssignmentUpdated",
      "description": "Updated test assignment",
      "stepId": "'$STEP_ID'",
      "entityIds": ["'$ADDRESS_ID'"]
    }'
    response=$(curl -s -w "%{http_code}" -X PUT \
      -H "Content-Type: application/json" \
      -d "$VALID_UPDATE_JSON" \
      -o /tmp/response.json \
      "$BASE_URL/$ASSIGNMENT_ID")
    print_result "200" "$response" "Valid update"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

# 11. DELETE /api/assignments/{id} - Delete assignment
print_test "11. DELETE /api/assignments/{id} - Delete assignment"

echo "Testing 404 Not Found - Non-existent ID"
response=$(curl -s -w "%{http_code}" -X DELETE \
  -o /tmp/response.json \
  "$BASE_URL/$NON_EXISTENT_ID")
print_result "404" "$response" "Non-existent ID"
echo "Response body:"
cat /tmp/response.json
echo ""

echo "Testing 204 No Content - Valid deletion"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" -X DELETE \
      -o /tmp/response.json \
      "$BASE_URL/$ASSIGNMENT_ID")
    print_result "204" "$response" "Valid deletion"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

echo "Testing 404 Not Found - Already deleted assignment"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    response=$(curl -s -w "%{http_code}" -X DELETE \
      -o /tmp/response.json \
      "$BASE_URL/$ASSIGNMENT_ID")
    print_result "404" "$response" "Already deleted assignment"
    echo "Response body:"
    cat /tmp/response.json
    echo ""
fi

# Summary
echo "=========================================="
echo "ASSIGNMENTS CONTROLLER API TESTS COMPLETED"
echo "=========================================="
echo ""
echo "All tests have been executed. Check the results above for any failures."
echo ""
echo "Test entities created:"
echo "- Schema ID: $SCHEMA_ID"
echo "- Processor ID: $PROCESSOR_ID"
echo "- Address ID: $ADDRESS_ID"
echo "- Step ID: $STEP_ID"
if [ ! -z "$ASSIGNMENT_ID" ]; then
    echo "- Assignment ID: $ASSIGNMENT_ID (created and deleted during tests)"
fi
echo ""
