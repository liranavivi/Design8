#!/bin/bash

# Test Report Generator for AssignmentsController API
# Generates comprehensive test reports in multiple formats

set -e

# Configuration
REPORT_DIR="test-reports"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
API_BASE_URL="http://localhost:5130"  # Local development URL

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Create report directory
mkdir -p "$REPORT_DIR"

echo -e "${BLUE}=== AssignmentsController API Test Report Generator ===${NC}"
echo -e "${BLUE}Generating comprehensive test report...${NC}"
echo ""

# Function to run test and capture output
run_test_with_capture() {
    local test_name="$1"
    local test_script="$2"
    local output_file="$REPORT_DIR/${test_name}_${TIMESTAMP}.log"
    
    echo -e "${BLUE}Running $test_name tests...${NC}"
    
    if [ -f "$test_script" ]; then
        chmod +x "$test_script"
        if ./"$test_script" > "$output_file" 2>&1; then
            echo -e "${GREEN}✓ $test_name tests completed${NC}"
            return 0
        else
            echo -e "${RED}✗ $test_name tests failed${NC}"
            return 1
        fi
    else
        echo -e "${YELLOW}⚠ $test_script not found${NC}"
        return 1
    fi
}

# Function to generate HTML report
generate_html_report() {
    local html_file="$REPORT_DIR/test_report_${TIMESTAMP}.html"
    
    cat > "$html_file" << EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AssignmentsController API Test Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { text-align: center; color: #333; border-bottom: 2px solid #007acc; padding-bottom: 20px; margin-bottom: 30px; }
        .test-section { margin: 20px 0; padding: 15px; border-left: 4px solid #007acc; background: #f9f9f9; }
        .test-section h3 { margin-top: 0; color: #007acc; }
        .pass { color: #28a745; font-weight: bold; }
        .fail { color: #dc3545; font-weight: bold; }
        .warning { color: #ffc107; font-weight: bold; }
        .summary { background: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .endpoint-list { background: white; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .endpoint-list ul { margin: 0; padding-left: 20px; }
        .status-code { background: #007acc; color: white; padding: 2px 6px; border-radius: 3px; font-size: 0.9em; }
        pre { background: #f8f9fa; padding: 10px; border-radius: 5px; overflow-x: auto; }
        .timestamp { color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🧪 AssignmentsController API Test Report</h1>
            <p class="timestamp">Generated on: $(date)</p>
            <p>API Base URL: <code>$API_BASE_URL</code></p>
        </div>

        <div class="summary">
            <h2>📊 Test Summary</h2>
            <p>This report covers comprehensive testing of all AssignmentsController endpoints including:</p>
            <ul>
                <li><strong>Core API Tests:</strong> All 11 endpoints with status code validation</li>
                <li><strong>Edge Case Tests:</strong> Validation, content type, and pagination scenarios</li>
                <li><strong>Performance Tests:</strong> Response time and load testing</li>
                <li><strong>JSON Validation:</strong> Response structure and data type validation</li>
                <li><strong>Security Tests:</strong> Basic security vulnerability testing</li>
            </ul>
        </div>

        <div class="test-section">
            <h3>🎯 Endpoints Tested</h3>
            <div class="endpoint-list">
                <ul>
                    <li><strong>GET</strong> /api/assignments → <span class="status-code">200</span> <span class="status-code">500</span></li>
                    <li><strong>GET</strong> /api/assignments/paged → <span class="status-code">200</span> <span class="status-code">400</span></li>
                    <li><strong>GET</strong> /api/assignments/{id} → <span class="status-code">200</span> <span class="status-code">404</span></li>
                    <li><strong>GET</strong> /api/assignments/by-key/{stepId} → <span class="status-code">200</span> <span class="status-code">404</span></li>
                    <li><strong>GET</strong> /api/assignments/by-step/{stepId} → <span class="status-code">200</span> <span class="status-code">404</span></li>
                    <li><strong>GET</strong> /api/assignments/by-entity/{entityId} → <span class="status-code">200</span></li>
                    <li><strong>GET</strong> /api/assignments/by-name/{name} → <span class="status-code">200</span> <span class="status-code">400</span></li>
                    <li><strong>GET</strong> /api/assignments/by-version/{version} → <span class="status-code">200</span> <span class="status-code">400</span></li>
                    <li><strong>POST</strong> /api/assignments → <span class="status-code">201</span> <span class="status-code">400</span> <span class="status-code">409</span></li>
                    <li><strong>PUT</strong> /api/assignments/{id} → <span class="status-code">200</span> <span class="status-code">400</span> <span class="status-code">404</span> <span class="status-code">409</span></li>
                    <li><strong>DELETE</strong> /api/assignments/{id} → <span class="status-code">204</span> <span class="status-code">404</span> <span class="status-code">409</span></li>
                </ul>
            </div>
        </div>
EOF

    # Add test results sections
    for test_type in "core" "edge" "performance" "json" "security"; do
        local log_file="$REPORT_DIR/${test_type}_${TIMESTAMP}.log"
        if [ -f "$log_file" ]; then
            cat >> "$html_file" << EOF
        <div class="test-section">
            <h3>📋 $(echo ${test_type^}) Test Results</h3>
            <pre>$(cat "$log_file")</pre>
        </div>
EOF
        fi
    done

    cat >> "$html_file" << EOF
        <div class="test-section">
            <h3>🔧 How to Run Tests</h3>
            <pre>
# Run all tests
./run-api-tests.sh

# Run specific test types
./run-api-tests.sh core        # Core API tests
./run-api-tests.sh edge        # Edge case tests
./run-api-tests.sh performance # Performance tests
./run-api-tests.sh json        # JSON validation tests
./run-api-tests.sh security    # Security tests

# Utility commands
./run-api-tests.sh status      # Show service status
./run-api-tests.sh logs        # Show service logs
./run-api-tests.sh cleanup     # Clean up Docker resources
            </pre>
        </div>

        <div class="test-section">
            <h3>📚 Documentation</h3>
            <p>For detailed documentation, see: <code>README-API-TESTING.md</code></p>
        </div>
    </div>
</body>
</html>
EOF

    echo -e "${GREEN}HTML report generated: $html_file${NC}"
}

# Function to generate JSON report
generate_json_report() {
    local json_file="$REPORT_DIR/test_report_${TIMESTAMP}.json"
    
    cat > "$json_file" << EOF
{
    "report": {
        "timestamp": "$(date -Iseconds)",
        "api_base_url": "$API_BASE_URL",
        "test_types": [
            {
                "name": "core",
                "description": "Core API functionality tests",
                "log_file": "core_${TIMESTAMP}.log"
            },
            {
                "name": "edge",
                "description": "Edge case and validation tests",
                "log_file": "edge_${TIMESTAMP}.log"
            },
            {
                "name": "performance",
                "description": "Performance and load tests",
                "log_file": "performance_${TIMESTAMP}.log"
            },
            {
                "name": "json",
                "description": "JSON validation tests",
                "log_file": "json_${TIMESTAMP}.log"
            },
            {
                "name": "security",
                "description": "Security vulnerability tests",
                "log_file": "security_${TIMESTAMP}.log"
            }
        ],
        "endpoints": [
            {"method": "GET", "path": "/api/assignments", "status_codes": [200, 500]},
            {"method": "GET", "path": "/api/assignments/paged", "status_codes": [200, 400]},
            {"method": "GET", "path": "/api/assignments/{id}", "status_codes": [200, 404]},
            {"method": "GET", "path": "/api/assignments/by-key/{stepId}", "status_codes": [200, 404]},
            {"method": "GET", "path": "/api/assignments/by-step/{stepId}", "status_codes": [200, 404]},
            {"method": "GET", "path": "/api/assignments/by-entity/{entityId}", "status_codes": [200]},
            {"method": "GET", "path": "/api/assignments/by-name/{name}", "status_codes": [200, 400]},
            {"method": "GET", "path": "/api/assignments/by-version/{version}", "status_codes": [200, 400]},
            {"method": "POST", "path": "/api/assignments", "status_codes": [201, 400, 409]},
            {"method": "PUT", "path": "/api/assignments/{id}", "status_codes": [200, 400, 404, 409]},
            {"method": "DELETE", "path": "/api/assignments/{id}", "status_codes": [204, 404, 409]}
        ]
    }
}
EOF

    echo -e "${GREEN}JSON report generated: $json_file${NC}"
}

# Main execution
echo -e "${BLUE}Checking if API is ready...${NC}"
if ! curl -s -f "$API_BASE_URL/health" > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠ API is not ready. Starting Docker services...${NC}"
    echo -e "${BLUE}Run './run-api-tests.sh' to start services and run tests.${NC}"
    exit 1
fi

# Run all test types and capture output
run_test_with_capture "core" "test-assignments-api.sh"
run_test_with_capture "edge" "test-assignments-edge-cases.sh"
run_test_with_capture "performance" "test-assignments-performance.sh"
run_test_with_capture "json" "test-assignments-json-validation.sh"
run_test_with_capture "security" "test-assignments-security.sh"

# Generate reports
echo ""
echo -e "${BLUE}Generating reports...${NC}"
generate_html_report
generate_json_report

echo ""
echo -e "${GREEN}=== Test Report Generation Complete ===${NC}"
echo -e "Reports generated in: ${CYAN}$REPORT_DIR/${NC}"
echo -e "• HTML Report: ${CYAN}test_report_${TIMESTAMP}.html${NC}"
echo -e "• JSON Report: ${CYAN}test_report_${TIMESTAMP}.json${NC}"
echo -e "• Individual logs: ${CYAN}*_${TIMESTAMP}.log${NC}"
echo ""
echo -e "${BLUE}Open the HTML report in your browser to view the complete test results.${NC}"
