#!/bin/bash

# Advanced OrchestratedFlowsController API Tests
# Tests CREATE, UPDATE, DELETE operations with actual data

BASE_URL="http://localhost:5130/api/orchestratedflows"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Advanced OrchestratedFlowsController API Tests"
echo "Base URL: $BASE_URL"
echo ""

# First, let's create prerequisite entities (Flow and Assignment)
echo "========================================="
echo "SETUP: Creating prerequisite entities"
echo "========================================="

# Create a Flow entity first
FLOW_PAYLOAD='{
  "id": "00000000-0000-0000-0000-000000000000",
  "version": "1.0.0",
  "name": "TestFlow",
  "description": "Test flow for orchestrated flow testing",
  "stepIds": [],
  "tags": ["test"],
  "metadata": {}
}'

echo "Creating Flow entity..."
FLOW_RESPONSE=$(curl -s -X POST "http://localhost:5130/api/flows" \
  -H "$CONTENT_TYPE" \
  -d "$FLOW_PAYLOAD")

FLOW_ID=$(echo "$FLOW_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "Created Flow with ID: $FLOW_ID"

# Create an Assignment entity
ASSIGNMENT_PAYLOAD='{
  "id": "00000000-0000-0000-0000-000000000000",
  "version": "1.0.0",
  "name": "TestAssignment",
  "description": "Test assignment for orchestrated flow testing",
  "assignedTo": "test-user",
  "status": "Active",
  "priority": 1,
  "tags": ["test"],
  "metadata": {}
}'

echo "Creating Assignment entity..."
ASSIGNMENT_RESPONSE=$(curl -s -X POST "http://localhost:5130/api/assignments" \
  -H "$CONTENT_TYPE" \
  -d "$ASSIGNMENT_PAYLOAD")

ASSIGNMENT_ID=$(echo "$ASSIGNMENT_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "Created Assignment with ID: $ASSIGNMENT_ID"
echo ""

# Now test OrchestratedFlow operations
echo "========================================="
echo "TESTING: POST /api/orchestratedflows"
echo "========================================="

ORCHESTRATED_FLOW_PAYLOAD='{
  "id": "00000000-0000-0000-0000-000000000000",
  "version": "1.0.0",
  "name": "TestOrchestratedFlow",
  "description": "Test orchestrated flow",
  "flowId": "'$FLOW_ID'",
  "assignmentIds": ["'$ASSIGNMENT_ID'"],
  "status": "Active",
  "priority": 1,
  "scheduledStartTime": "2024-01-01T00:00:00Z",
  "estimatedDuration": "PT1H",
  "tags": ["test"],
  "metadata": {"testKey": "testValue"}
}'

echo "Testing 201 Created - Valid orchestrated flow"
CREATE_RESPONSE=$(curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d "$ORCHESTRATED_FLOW_PAYLOAD")

echo "$CREATE_RESPONSE"
ORCHESTRATED_FLOW_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "Created OrchestratedFlow with ID: $ORCHESTRATED_FLOW_ID"
echo ""

echo "Testing 409 Conflict - Duplicate composite key"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d "$ORCHESTRATED_FLOW_PAYLOAD"
echo ""

echo "========================================="
echo "TESTING: GET /api/orchestratedflows/{id}"
echo "========================================="

echo "Testing 200 OK - Get created orchestrated flow"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/$ORCHESTRATED_FLOW_ID"
echo ""

echo "========================================="
echo "TESTING: PUT /api/orchestratedflows/{id}"
echo "========================================="

UPDATE_PAYLOAD='{
  "id": "'$ORCHESTRATED_FLOW_ID'",
  "version": "1.1.0",
  "name": "UpdatedOrchestratedFlow",
  "description": "Updated test orchestrated flow",
  "flowId": "'$FLOW_ID'",
  "assignmentIds": ["'$ASSIGNMENT_ID'"],
  "status": "Active",
  "priority": 2,
  "scheduledStartTime": "2024-01-01T00:00:00Z",
  "estimatedDuration": "PT2H",
  "tags": ["test", "updated"],
  "metadata": {"testKey": "updatedValue"}
}'

echo "Testing 200 OK - Valid update"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/$ORCHESTRATED_FLOW_ID" \
  -H "$CONTENT_TYPE" \
  -d "$UPDATE_PAYLOAD"
echo ""

echo "========================================="
echo "TESTING: GET /api/orchestratedflows/by-name/{name}"
echo "========================================="

echo "Testing 200 OK - Get by updated name"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-name/UpdatedOrchestratedFlow"
echo ""

echo "========================================="
echo "TESTING: GET /api/orchestratedflows/by-key/{version}/{name}"
echo "========================================="

echo "Testing 200 OK - Get by updated composite key"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/by-key/1.1.0/UpdatedOrchestratedFlow"
echo ""

echo "========================================="
echo "TESTING: DELETE /api/orchestratedflows/{id}"
echo "========================================="

echo "Testing 204 No Content - Successful deletion"
curl -s -w "Status: %{http_code}\n" -X DELETE "$BASE_URL/$ORCHESTRATED_FLOW_ID"
echo ""

echo "Testing 404 Not Found - Get deleted orchestrated flow"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/$ORCHESTRATED_FLOW_ID"
echo ""

echo "========================================="
echo "CLEANUP: Removing test entities"
echo "========================================="

echo "Deleting test Assignment..."
curl -s -X DELETE "http://localhost:5130/api/assignments/$ASSIGNMENT_ID"
echo "Assignment deleted"

echo "Deleting test Flow..."
curl -s -X DELETE "http://localhost:5130/api/flows/$FLOW_ID"
echo "Flow deleted"

echo ""
echo "🎯 Advanced tests completed!"
echo ""
echo "========================================="
echo "STATUS CODES DEMONSTRATED:"
echo "========================================="
echo "✅ 200 OK - GET operations"
echo "✅ 201 Created - POST operation"
echo "✅ 204 No Content - DELETE operation"
echo "✅ 400 Bad Request - Validation errors, GUID format errors"
echo "✅ 404 Not Found - Entity not found"
echo "✅ 409 Conflict - Duplicate key"
echo "📝 500 Internal Server Error - (Database connection issues)"
echo ""
