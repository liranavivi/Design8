#!/bin/bash

# Verification Script: EntityId → ProcessorId Migration
# This script verifies that all changes have been applied correctly

echo "🔍 MIGRATION VERIFICATION: EntityId → ProcessorId"
echo "=================================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Verification counters
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0

# Function to print verification results
print_verification_result() {
    local check_name="$1"
    local expected="$2"
    local actual="$3"
    
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    
    if [ "$actual" = "$expected" ]; then
        echo -e "${GREEN}✅ PASS${NC} - $check_name"
        PASSED_CHECKS=$((PASSED_CHECKS + 1))
    else
        echo -e "${RED}❌ FAIL${NC} - $check_name (Expected: $expected, Found: $actual)"
        FAILED_CHECKS=$((FAILED_CHECKS + 1))
    fi
}

echo "🔍 VERIFYING CODE CHANGES"
echo "========================="

# 1. Verify StepEntity class changes
echo "1. Checking StepEntity class..."
if grep -q "public Guid ProcessorId" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/StepEntity.cs"; then
    print_verification_result "StepEntity.ProcessorId property exists" "found" "found"
else
    print_verification_result "StepEntity.ProcessorId property exists" "found" "not found"
fi

if grep -q 'BsonElement("processorId")' "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/StepEntity.cs"; then
    print_verification_result "StepEntity BSON mapping updated" "found" "found"
else
    print_verification_result "StepEntity BSON mapping updated" "found" "not found"
fi

if ! grep -q "EntityId" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/StepEntity.cs"; then
    print_verification_result "StepEntity.EntityId removed" "removed" "removed"
else
    print_verification_result "StepEntity.EntityId removed" "removed" "still exists"
fi

# 2. Verify Repository changes
echo ""
echo "2. Checking Repository changes..."
if grep -q "GetByProcessorIdAsync" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Interfaces/Repositories/IStepEntityRepository.cs"; then
    print_verification_result "IStepEntityRepository.GetByProcessorIdAsync exists" "found" "found"
else
    print_verification_result "IStepEntityRepository.GetByProcessorIdAsync exists" "found" "not found"
fi

if grep -q "step_processorid_idx" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/StepEntityRepository.cs"; then
    print_verification_result "New database index name updated" "found" "found"
else
    print_verification_result "New database index name updated" "found" "not found"
fi

if ! grep -q "GetByEntityIdAsync" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/StepEntityRepository.cs"; then
    print_verification_result "Old GetByEntityIdAsync method removed" "removed" "removed"
else
    print_verification_result "Old GetByEntityIdAsync method removed" "removed" "still exists"
fi

# 3. Verify API Controller changes
echo ""
echo "3. Checking API Controller changes..."
if grep -q "by-processor-id" "src/Presentation/FlowOrchestrator.EntitiesManagers.Api/Controllers/StepsController.cs"; then
    print_verification_result "API endpoint URL updated" "found" "found"
else
    print_verification_result "API endpoint URL updated" "found" "not found"
fi

if grep -q "GetByProcessorId" "src/Presentation/FlowOrchestrator.EntitiesManagers.Api/Controllers/StepsController.cs"; then
    print_verification_result "API method name updated" "found" "found"
else
    print_verification_result "API method name updated" "found" "not found"
fi

if ! grep -q "by-entity-id" "src/Presentation/FlowOrchestrator.EntitiesManagers.Api/Controllers/StepsController.cs"; then
    print_verification_result "Old API endpoint removed" "removed" "removed"
else
    print_verification_result "Old API endpoint removed" "removed" "still exists"
fi

# 4. Verify MassTransit changes
echo ""
echo "4. Checking MassTransit changes..."
if grep -q "public Guid ProcessorId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Commands/StepCommands.cs"; then
    print_verification_result "MassTransit Commands updated" "found" "found"
else
    print_verification_result "MassTransit Commands updated" "found" "not found"
fi

if grep -q "public Guid ProcessorId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Events/StepEvents.cs"; then
    print_verification_result "MassTransit Events updated" "found" "found"
else
    print_verification_result "MassTransit Events updated" "found" "not found"
fi

if grep -q "ProcessorId = context.Message.ProcessorId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Consumers/Step/CreateStepCommandConsumer.cs"; then
    print_verification_result "MassTransit Consumers updated" "found" "found"
else
    print_verification_result "MassTransit Consumers updated" "found" "not found"
fi

# 5. Verify Test Scripts
echo ""
echo "5. Checking Test Scripts..."
if grep -q "by-processor-id" "steps_comprehensive_tests.sh"; then
    print_verification_result "Comprehensive test script updated" "found" "found"
else
    print_verification_result "Comprehensive test script updated" "found" "not found"
fi

if grep -q "processorId" "steps_referential_integrity_test.sh"; then
    print_verification_result "Referential integrity test script updated" "found" "found"
else
    print_verification_result "Referential integrity test script updated" "found" "not found"
fi

# 6. Verify Documentation
echo ""
echo "6. Checking Documentation..."
if grep -q "ProcessorId" "steps_endpoints_summary.md"; then
    print_verification_result "Endpoints summary documentation updated" "found" "found"
else
    print_verification_result "Endpoints summary documentation updated" "found" "not found"
fi

if grep -q "by-processor-id" "steps_status_codes_matrix.md"; then
    print_verification_result "Status codes matrix documentation updated" "found" "found"
else
    print_verification_result "Status codes matrix documentation updated" "found" "not found"
fi

# 7. Verify Migration Files
echo ""
echo "7. Checking Migration Files..."
if [ -f "database_migration_entityid_to_processorid.js" ]; then
    print_verification_result "Database migration script exists" "found" "found"
else
    print_verification_result "Database migration script exists" "found" "not found"
fi

if [ -f "MIGRATION_SUMMARY_EntityId_to_ProcessorId.md" ]; then
    print_verification_result "Migration summary document exists" "found" "found"
else
    print_verification_result "Migration summary document exists" "found" "not found"
fi

echo ""
echo "🔍 VERIFYING NO LEGACY REFERENCES"
echo "=================================="

# Check for any remaining EntityId references in key files
LEGACY_REFS=0

echo "Checking for legacy 'EntityId' references..."
if grep -r "EntityId" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/StepEntity.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy EntityId in StepEntity${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

if grep -r "GetByEntityIdAsync" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Interfaces/Repositories/IStepEntityRepository.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy GetByEntityIdAsync in interface${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

if grep -r "by-entity-id" "src/Presentation/FlowOrchestrator.EntitiesManagers.Api/Controllers/StepsController.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy by-entity-id in controller${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

if [ $LEGACY_REFS -eq 0 ]; then
    echo -e "${GREEN}✅ No legacy EntityId references found${NC}"
    print_verification_result "Legacy references cleanup" "clean" "clean"
else
    echo -e "${RED}❌ Found $LEGACY_REFS legacy references${NC}"
    print_verification_result "Legacy references cleanup" "clean" "has legacy refs"
fi

echo ""
echo "📋 VERIFICATION SUMMARY"
echo "======================="
echo -e "${BLUE}Total Checks: $TOTAL_CHECKS${NC}"
echo -e "${GREEN}Passed: $PASSED_CHECKS${NC}"
echo -e "${RED}Failed: $FAILED_CHECKS${NC}"

if [ $FAILED_CHECKS -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 MIGRATION VERIFICATION SUCCESSFUL! 🎉${NC}"
    echo -e "${GREEN}✅ All changes applied correctly${NC}"
    echo -e "${GREEN}✅ No legacy references found${NC}"
    echo -e "${GREEN}✅ Ready for deployment${NC}"
    echo ""
    echo "📋 NEXT STEPS:"
    echo "1. Run database migration script: database_migration_entityid_to_processorid.js"
    echo "2. Deploy updated application code"
    echo "3. Run comprehensive tests: ./steps_comprehensive_tests.sh"
    echo "4. Verify referential integrity: ./steps_referential_integrity_test.sh"
    exit 0
else
    echo ""
    echo -e "${RED}❌ MIGRATION VERIFICATION FAILED${NC}"
    echo -e "${RED}Please review and fix the failed checks above${NC}"
    exit 1
fi
