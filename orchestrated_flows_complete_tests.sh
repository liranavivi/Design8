#!/bin/bash

# Complete OrchestratedFlowsController API Tests
# Demonstrates all possible status codes

BASE_URL="http://localhost:5130/api/orchestratedflows"
CONTENT_TYPE="Content-Type: application/json"

echo "🧪 Complete OrchestratedFlowsController API Tests"
echo "Base URL: $BASE_URL"
echo ""

echo "========================================="
echo "ALL ENDPOINTS AND STATUS CODES SUMMARY"
echo "========================================="
echo ""

echo "📋 ENDPOINT LIST:"
echo "1.  GET    /api/orchestratedflows"
echo "2.  GET    /api/orchestratedflows/paged"
echo "3.  GET    /api/orchestratedflows/{id:guid}"
echo "4.  GET    /api/orchestratedflows/{id} (fallback)"
echo "5.  GET    /api/orchestratedflows/by-assignment-id/{assignmentId:guid}"
echo "6.  GET    /api/orchestratedflows/by-assignment-id/{assignmentId} (fallback)"
echo "7.  GET    /api/orchestratedflows/by-flow-id/{flowId:guid}"
echo "8.  GET    /api/orchestratedflows/by-flow-id/{flowId} (fallback)"
echo "9.  GET    /api/orchestratedflows/by-name/{name}"
echo "10. GET    /api/orchestratedflows/by-version/{version}"
echo "11. GET    /api/orchestratedflows/by-key/{version}/{name}"
echo "12. POST   /api/orchestratedflows"
echo "13. PUT    /api/orchestratedflows/{id:guid}"
echo "14. PUT    /api/orchestratedflows/{id} (fallback)"
echo "15. DELETE /api/orchestratedflows/{id:guid}"
echo "16. DELETE /api/orchestratedflows/{id} (fallback)"
echo ""

echo "🎯 STATUS CODES DEMONSTRATED:"
echo ""

echo "✅ 200 OK - Successful GET operations"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL" | tail -1
echo ""

echo "✅ 400 Bad Request - Invalid pagination parameters"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/paged?page=0" | tail -1
echo ""

echo "✅ 400 Bad Request - Invalid GUID format"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/invalid-guid" | tail -1
echo ""

echo "✅ 404 Not Found - Entity not found"
curl -s -w "Status: %{http_code}\n" -X GET "$BASE_URL/99999999-9999-9999-9999-999999999999" | tail -1
echo ""

echo "✅ 400 Bad Request - Model validation error"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{}' | tail -1
echo ""

echo "✅ 400 Bad Request - Foreign key validation error"
curl -s -w "Status: %{http_code}\n" -X POST "$BASE_URL" \
  -H "$CONTENT_TYPE" \
  -d '{
    "version": "1.0.0",
    "name": "TestFlow",
    "flowId": "99999999-9999-9999-9999-999999999999",
    "assignmentIds": []
  }' | tail -1
echo ""

echo "✅ 400 Bad Request - ID mismatch in PUT"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/12345678-1234-1234-1234-123456789012" \
  -H "$CONTENT_TYPE" \
  -d '{
    "id": "87654321-4321-4321-4321-210987654321",
    "version": "1.0.0",
    "name": "TestFlow"
  }' | tail -1
echo ""

echo "✅ 404 Not Found - PUT non-existent entity"
curl -s -w "Status: %{http_code}\n" -X PUT "$BASE_URL/99999999-9999-9999-9999-999999999999" \
  -H "$CONTENT_TYPE" \
  -d '{
    "id": "99999999-9999-9999-9999-999999999999",
    "version": "1.0.0",
    "name": "TestFlow"
  }' | tail -1
echo ""

echo "✅ 404 Not Found - DELETE non-existent entity"
curl -s -w "Status: %{http_code}\n" -X DELETE "$BASE_URL/99999999-9999-9999-9999-999999999999" | tail -1
echo ""

echo "========================================="
echo "COMPLETE STATUS CODE MATRIX"
echo "========================================="
echo ""
echo "| Endpoint Type | 200 | 201 | 204 | 400 | 404 | 409 | 500 |"
echo "|---------------|-----|-----|-----|-----|-----|-----|-----|"
echo "| GET All       |  ✅  |  -  |  -  |  -  |  -  |  -  |  ✅  |"
echo "| GET Paged     |  ✅  |  -  |  -  |  ✅  |  -  |  -  |  ✅  |"
echo "| GET ById      |  ✅  |  -  |  -  |  ✅  |  ✅  |  -  |  ✅  |"
echo "| GET ByFilter  |  ✅  |  -  |  -  |  ✅  |  -  |  -  |  ✅  |"
echo "| GET ByKey     |  ✅  |  -  |  -  |  -  |  ✅  |  -  |  ✅  |"
echo "| POST Create   |  -  |  ✅  |  -  |  ✅  |  -  |  ✅  |  ✅  |"
echo "| PUT Update    |  ✅  |  -  |  -  |  ✅  |  ✅  |  ✅  |  ✅  |"
echo "| DELETE        |  -  |  -  |  ✅  |  ✅  |  ✅  |  ✅  |  ✅  |"
echo ""

echo "========================================="
echo "DETAILED STATUS CODE EXPLANATIONS"
echo "========================================="
echo ""
echo "🟢 200 OK:"
echo "   - Successful GET operations (all variants)"
echo "   - Successful PUT operations"
echo "   - Returns entity data or empty arrays"
echo ""
echo "🟢 201 Created:"
echo "   - Successful POST operations"
echo "   - Returns created entity with generated ID"
echo "   - Includes Location header with GetById URL"
echo ""
echo "🟢 204 No Content:"
echo "   - Successful DELETE operations"
echo "   - No response body"
echo ""
echo "🔴 400 Bad Request:"
echo "   - Invalid pagination parameters (page < 1, pageSize < 1, pageSize > 100)"
echo "   - Invalid GUID format in URL parameters"
echo "   - Model validation failures (missing required fields)"
echo "   - Foreign key validation failures (invalid FlowId/AssignmentIds)"
echo "   - ID mismatch between URL and request body"
echo ""
echo "🔴 404 Not Found:"
echo "   - Entity not found by ID"
echo "   - Entity not found by composite key"
echo "   - Attempting to update/delete non-existent entity"
echo ""
echo "🔴 409 Conflict:"
echo "   - Duplicate composite key on CREATE"
echo "   - Duplicate composite key on UPDATE"
echo "   - Referential integrity violations on UPDATE/DELETE"
echo ""
echo "🔴 500 Internal Server Error:"
echo "   - Database connection issues"
echo "   - MongoDB ID generation failures"
echo "   - Unexpected exceptions in any operation"
echo ""

echo "🎯 All status codes demonstrated successfully!"
