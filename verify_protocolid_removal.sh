#!/bin/bash

# Verification Script: ProtocolId Removal from ProcessorEntity
# This script verifies that all ProtocolId references have been removed correctly

echo "🔍 MIGRATION VERIFICATION: Remove ProtocolId from ProcessorEntity"
echo "=================================================================="
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

# 1. Verify ProcessorEntity class changes
echo "1. Checking ProcessorEntity class..."
if ! grep -q "public Guid ProtocolId" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/ProcessorEntity.cs"; then
    print_verification_result "ProcessorEntity.ProtocolId property removed" "removed" "removed"
else
    print_verification_result "ProcessorEntity.ProtocolId property removed" "removed" "still exists"
fi

if ! grep -q 'BsonElement("protocolId")' "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/ProcessorEntity.cs"; then
    print_verification_result "ProcessorEntity BSON mapping removed" "removed" "removed"
else
    print_verification_result "ProcessorEntity BSON mapping removed" "removed" "still exists"
fi

if ! grep -q "ProtocolId is required" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/ProcessorEntity.cs"; then
    print_verification_result "ProcessorEntity validation attribute removed" "removed" "removed"
else
    print_verification_result "ProcessorEntity validation attribute removed" "removed" "still exists"
fi

# 2. Verify Repository changes
echo ""
echo "2. Checking Repository changes..."
if ! grep -q "ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/ProcessorEntityRepository.cs"; then
    print_verification_result "Repository ProtocolId references removed" "removed" "removed"
else
    print_verification_result "Repository ProtocolId references removed" "removed" "still exists"
fi

if ! grep -q "x.ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/ProcessorEntityRepository.cs"; then
    print_verification_result "Repository ProtocolId index removed" "removed" "removed"
else
    print_verification_result "Repository ProtocolId index removed" "removed" "still exists"
fi

# 3. Verify MassTransit changes
echo ""
echo "3. Checking MassTransit changes..."
if ! grep -q "public Guid ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Commands/ProcessorCommands.cs"; then
    print_verification_result "MassTransit Commands ProtocolId removed" "removed" "removed"
else
    print_verification_result "MassTransit Commands ProtocolId removed" "removed" "still exists"
fi

if ! grep -q "public Guid ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Events/ProcessorEvents.cs"; then
    print_verification_result "MassTransit Events ProtocolId removed" "removed" "removed"
else
    print_verification_result "MassTransit Events ProtocolId removed" "removed" "still exists"
fi

if ! grep -q "ProtocolId = context.Message.ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Consumers/Processor/CreateProcessorCommandConsumer.cs"; then
    print_verification_result "MassTransit Consumers ProtocolId removed" "removed" "removed"
else
    print_verification_result "MassTransit Consumers ProtocolId removed" "removed" "still exists"
fi

# 4. Verify API Controller changes
echo ""
echo "4. Checking API Controller changes..."
if ! grep -q "ProtocolId.*User.*RequestId" "src/Presentation/FlowOrchestrator.EntitiesManagers.Api/Controllers/ProcessorsController.cs"; then
    print_verification_result "API Controller ProtocolId logging removed" "removed" "removed"
else
    print_verification_result "API Controller ProtocolId logging removed" "removed" "still exists"
fi

# 5. Verify Test Scripts
echo ""
echo "5. Checking Test Scripts..."
if ! grep -q '"protocolId"' "processors_comprehensive_tests.sh"; then
    print_verification_result "Comprehensive test script ProtocolId removed" "removed" "removed"
else
    print_verification_result "Comprehensive test script ProtocolId removed" "removed" "still exists"
fi

if ! grep -q '"protocolId"' "processors_api_tests.sh"; then
    print_verification_result "API test script ProtocolId removed" "removed" "removed"
else
    print_verification_result "API test script ProtocolId removed" "removed" "still exists"
fi

# 6. Verify Documentation
echo ""
echo "6. Checking Documentation..."
if ! grep -q "ProtocolId.*NOT validated" "processors_endpoints_summary.md"; then
    print_verification_result "Documentation ProtocolId references updated" "updated" "updated"
else
    print_verification_result "Documentation ProtocolId references updated" "updated" "still has old refs"
fi

# 7. Verify Migration Files
echo ""
echo "7. Checking Migration Files..."
if [ -f "database_migration_remove_protocolid.js" ]; then
    print_verification_result "Database migration script exists" "found" "found"
else
    print_verification_result "Database migration script exists" "found" "not found"
fi

if [ -f "MIGRATION_SUMMARY_Remove_ProtocolId.md" ]; then
    print_verification_result "Migration summary document exists" "found" "found"
else
    print_verification_result "Migration summary document exists" "found" "not found"
fi

echo ""
echo "🔍 VERIFYING NO LEGACY REFERENCES"
echo "=================================="

# Check for any remaining ProtocolId references in key files
LEGACY_REFS=0

echo "Checking for legacy 'ProtocolId' references..."

# Check entity class
if grep -q "ProtocolId" "src/Core/FlowOrchestrator.EntitiesManagers.Core/Entities/ProcessorEntity.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy ProtocolId in ProcessorEntity${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

# Check repository
if grep -q "ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/ProcessorEntityRepository.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy ProtocolId in ProcessorEntityRepository${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

# Check MassTransit commands
if grep -q "ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Commands/ProcessorCommands.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy ProtocolId in ProcessorCommands${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

# Check MassTransit events
if grep -q "ProtocolId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/MassTransit/Events/ProcessorEvents.cs" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy ProtocolId in ProcessorEvents${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

# Check test scripts for JSON protocolId
if grep -q '"protocolId"' "processors_comprehensive_tests.sh" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy protocolId in comprehensive tests${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

if grep -q '"protocolId"' "processors_api_tests.sh" 2>/dev/null; then
    echo -e "${RED}❌ Found legacy protocolId in API tests${NC}"
    LEGACY_REFS=$((LEGACY_REFS + 1))
fi

if [ $LEGACY_REFS -eq 0 ]; then
    echo -e "${GREEN}✅ No legacy ProtocolId references found${NC}"
    print_verification_result "Legacy references cleanup" "clean" "clean"
else
    echo -e "${RED}❌ Found $LEGACY_REFS legacy references${NC}"
    print_verification_result "Legacy references cleanup" "clean" "has legacy refs"
fi

echo ""
echo "🔍 VERIFYING FOREIGN KEY VALIDATION STILL WORKS"
echo "==============================================="

# Check that InputSchemaId and OutputSchemaId validation is still present
if grep -q "ValidateProcessorEntityForeignKeysAsync.*inputSchemaId.*outputSchemaId" "src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/Repositories/ProcessorEntityRepository.cs"; then
    print_verification_result "Foreign key validation still works" "working" "working"
else
    print_verification_result "Foreign key validation still works" "working" "not found"
fi

echo ""
echo "📋 VERIFICATION SUMMARY"
echo "======================="
echo -e "${BLUE}Total Checks: $TOTAL_CHECKS${NC}"
echo -e "${GREEN}Passed: $PASSED_CHECKS${NC}"
echo -e "${RED}Failed: $FAILED_CHECKS${NC}"

if [ $FAILED_CHECKS -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 PROTOCOLID REMOVAL VERIFICATION SUCCESSFUL! 🎉${NC}"
    echo -e "${GREEN}✅ All ProtocolId references removed correctly${NC}"
    echo -e "${GREEN}✅ No legacy references found${NC}"
    echo -e "${GREEN}✅ Foreign key validation maintained${NC}"
    echo -e "${GREEN}✅ Ready for deployment${NC}"
    echo ""
    echo "📋 NEXT STEPS:"
    echo "1. Run database migration script: database_migration_remove_protocolid.js"
    echo "2. Deploy updated application code"
    echo "3. Run comprehensive tests: ./processors_comprehensive_tests.sh"
    echo "4. Update BaseProcessor framework to remove ProtocolId dependency"
    echo ""
    echo "⚠️  BREAKING CHANGE WARNING:"
    echo "This change removes ProtocolId from API responses and MassTransit events."
    echo "Ensure all dependent systems are updated before deployment!"
    exit 0
else
    echo ""
    echo -e "${RED}❌ PROTOCOLID REMOVAL VERIFICATION FAILED${NC}"
    echo -e "${RED}Please review and fix the failed checks above${NC}"
    exit 1
fi
