#!/bin/bash

# OrchestratedFlowsController API Tests
# Tests all endpoints with various status codes

BASE_URL="http://localhost:5130/api/orchestratedflows"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 OrchestratedFlowsController API Tests"
echo "Base URL: $BASE_URL"
echo ""

# Check if API is running
echo "Checking if API is running..."
if curl -s -f "$BASE_URL" > /dev/null 2>&1; then
    echo "✅ API is running"
else
    echo "❌ API is not running. Please start the API first."
    exit 1
fi

echo ""
echo "========================================="
echo "ENDPOINT 1: GET /api/orchestratedflows"
echo "========================================="

echo "Testing 200 OK - Get all orchestrated flows"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL"
echo ""

echo "Testing 500 Internal Server Error (simulated by stopping database)"
# This would require stopping the database, so we'll just document it
echo "# To test 500: Stop MongoDB and retry the request"
echo ""

echo "========================================="
echo "ENDPOINT 2: GET /api/orchestratedflows/paged"
echo "========================================="

echo "Testing 200 OK - Get paged orchestrated flows (default)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged"
echo ""

echo "Testing 200 OK - Get paged orchestrated flows (custom page/size)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?page=1&pageSize=5"
echo ""

echo "Testing 400 Bad Request - Invalid page (0)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?page=0"
echo ""

echo "Testing 400 Bad Request - Invalid page (negative)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?page=-1"
echo ""

echo "Testing 400 Bad Request - Invalid pageSize (0)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?pageSize=0"
echo ""

echo "Testing 400 Bad Request - Invalid pageSize (negative)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?pageSize=-5"
echo ""

echo "Testing 400 Bad Request - PageSize exceeds maximum (>100)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?pageSize=101"
echo ""

echo "========================================="
echo "ENDPOINT 3: GET /api/orchestratedflows/{id:guid}"
echo "========================================="

# First, let's create a test orchestrated flow to get a valid ID
echo "Creating test orchestrated flow for ID tests..."
VALID_FLOW_ID="12345678-1234-1234-1234-123456789012"
VALID_ASSIGNMENT_ID="87654321-4321-4321-4321-210987654321"

TEST_FLOW='{
  "id": "00000000-0000-0000-0000-000000000000",
  "version": "1.0.0",
  "name": "TestOrchestratedFlow",
  "description": "Test orchestrated flow for API testing",
  "flowId": "'$VALID_FLOW_ID'",
  "assignmentIds": ["'$VALID_ASSIGNMENT_ID'"],
  "status": "Active",
  "priority": 1,
  "scheduledStartTime": "2024-01-01T00:00:00Z",
  "estimatedDuration": "PT1H",
  "tags": ["test", "api"],
  "metadata": {"testKey": "testValue"}
}'

echo "Test flow payload created"
echo ""

echo "Testing 404 Not Found - Non-existent GUID"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999"
echo ""

echo "Testing 400 Bad Request - Invalid GUID format"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/invalid-guid"
echo ""

echo "========================================="
echo "ENDPOINT 4: GET /api/orchestratedflows/by-assignment-id/{assignmentId:guid}"
echo "========================================="

echo "Testing 200 OK - Get by assignment ID (empty result)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-assignment-id/$VALID_ASSIGNMENT_ID"
echo ""

echo "Testing 400 Bad Request - Invalid assignment ID format"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-assignment-id/invalid-guid"
echo ""

echo "========================================="
echo "ENDPOINT 5: GET /api/orchestratedflows/by-flow-id/{flowId:guid}"
echo "========================================="

echo "Testing 200 OK - Get by flow ID (empty result)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-flow-id/$VALID_FLOW_ID"
echo ""

echo "Testing 400 Bad Request - Invalid flow ID format"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-flow-id/invalid-guid"
echo ""

echo "========================================="
echo "ENDPOINT 6: GET /api/orchestratedflows/by-name/{name}"
echo "========================================="

echo "Testing 200 OK - Get by name (empty result)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-name/NonExistentFlow"
echo ""

echo "Testing 200 OK - Get by name with URL encoding"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-name/Test%20Flow%20Name"
echo ""

echo "========================================="
echo "ENDPOINT 7: GET /api/orchestratedflows/by-version/{version}"
echo "========================================="

echo "Testing 200 OK - Get by version (empty result)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-version/1.0.0"
echo ""

echo "Testing 200 OK - Get by version with special characters"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-version/1.0.0-beta"
echo ""

echo "========================================="
echo "ENDPOINT 8: GET /api/orchestratedflows/by-key/{version}/{name}"
echo "========================================="

echo "Testing 404 Not Found - Non-existent composite key"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-key/1.0.0/NonExistentFlow"
echo ""

echo "Testing 200 OK - Valid composite key format (but non-existent)"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-key/2.0.0/TestFlow"
echo ""

echo ""
echo "========================================="
echo "ENDPOINT 9: POST /api/orchestratedflows"
echo "========================================="

echo "Testing 400 Bad Request - Invalid model (missing required fields)"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{}'
echo ""

echo "Testing 400 Bad Request - Invalid model (null name)"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{"version": "1.0.0", "name": null}'
echo ""

echo "Testing 400 Bad Request - Foreign key validation (invalid FlowId)"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{
    "version": "1.0.0",
    "name": "TestFlow",
    "flowId": "99999999-9999-9999-9999-999999999999",
    "assignmentIds": []
  }'
echo ""

echo "Testing 400 Bad Request - Foreign key validation (invalid AssignmentId)"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{
    "version": "1.0.0",
    "name": "TestFlow",
    "flowId": "'$VALID_FLOW_ID'",
    "assignmentIds": ["99999999-9999-9999-9999-999999999999"]
  }'
echo ""

echo "========================================="
echo "ENDPOINT 10: PUT /api/orchestratedflows/{id:guid}"
echo "========================================="

echo "Testing 400 Bad Request - Invalid GUID format"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/invalid-guid" \
  -H "$CONTENT_TYPE" \
  -d '{}'
echo ""

echo "Testing 404 Not Found - Non-existent ID"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/99999999-9999-9999-9999-999999999999" \
  -H "$CONTENT_TYPE" \
  -d '{
    "id": "99999999-9999-9999-9999-999999999999",
    "version": "1.0.0",
    "name": "TestFlow"
  }'
echo ""

echo "Testing 400 Bad Request - ID mismatch"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
  -H "$CONTENT_TYPE" \
  -d '{
    "id": "87654321-4321-4321-4321-210987654321",
    "version": "1.0.0",
    "name": "TestFlow"
  }'
echo ""

echo "========================================="
echo "ENDPOINT 11: DELETE /api/orchestratedflows/{id:guid}"
echo "========================================="

echo "Testing 400 Bad Request - Invalid GUID format"
curl -s -w "Status: %{http_code}\n" -X DELETE "$BASE_URL/invalid-guid"
echo ""

echo "Testing 404 Not Found - Non-existent ID"
curl -s -w "Status: %{http_code}\n" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999"
echo ""

echo ""
echo "========================================="
echo "SUMMARY OF ALL POSSIBLE STATUS CODES"
echo "========================================="
echo "200 OK - Successful GET operations"
echo "201 Created - Successful POST operations"
echo "204 No Content - Successful DELETE operations"
echo "400 Bad Request - Invalid input, validation errors, GUID format errors"
echo "404 Not Found - Entity not found"
echo "409 Conflict - Duplicate key, referential integrity violations"
echo "500 Internal Server Error - Database errors, unexpected exceptions"
echo ""
echo "🎯 Test completed!"
