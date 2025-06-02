// MongoDB Migration Script: Rename EntityId to ProcessorId in StepEntity
// This script renames the "entityId" field to "processorId" in the steps collection
// and updates the corresponding index

// Connect to the database
use entitiesmanager;

print("🚀 Starting migration: EntityId → ProcessorId in StepEntity");
print("Database: " + db.getName());
print("Collection: steps");
print("");

// Step 1: Check current collection state
print("📊 STEP 1: Analyzing current collection state");
var totalDocs = db.steps.countDocuments();
var docsWithEntityId = db.steps.countDocuments({ "entityId": { $exists: true } });
var docsWithProcessorId = db.steps.countDocuments({ "processorId": { $exists: true } });

print("Total documents in steps collection: " + totalDocs);
print("Documents with 'entityId' field: " + docsWithEntityId);
print("Documents with 'processorId' field: " + docsWithProcessorId);
print("");

if (docsWithEntityId === 0) {
    print("✅ No documents found with 'entityId' field. Migration may have already been completed.");
    print("Exiting migration script.");
    quit();
}

if (docsWithProcessorId > 0) {
    print("⚠️  WARNING: Found " + docsWithProcessorId + " documents with 'processorId' field.");
    print("This suggests partial migration. Please review manually before proceeding.");
    print("Exiting migration script for safety.");
    quit();
}

// Step 2: Create backup collection
print("💾 STEP 2: Creating backup collection");
try {
    db.steps.aggregate([
        { $match: {} }
    ]).forEach(function(doc) {
        db.steps_backup_entityid_migration.insertOne(doc);
    });
    
    var backupCount = db.steps_backup_entityid_migration.countDocuments();
    print("✅ Backup created successfully: " + backupCount + " documents backed up");
    print("Backup collection: steps_backup_entityid_migration");
} catch (error) {
    print("❌ ERROR creating backup: " + error);
    quit();
}
print("");

// Step 3: Rename field from entityId to processorId
print("🔄 STEP 3: Renaming field 'entityId' to 'processorId'");
try {
    var renameResult = db.steps.updateMany(
        { "entityId": { $exists: true } },
        { $rename: { "entityId": "processorId" } }
    );
    
    print("✅ Field rename completed successfully");
    print("Documents modified: " + renameResult.modifiedCount);
    print("Documents matched: " + renameResult.matchedCount);
} catch (error) {
    print("❌ ERROR during field rename: " + error);
    print("Attempting to restore from backup...");
    
    // Restore from backup
    try {
        db.steps.deleteMany({});
        db.steps_backup_entityid_migration.find().forEach(function(doc) {
            delete doc._id; // Remove _id to avoid conflicts
            db.steps.insertOne(doc);
        });
        print("✅ Backup restored successfully");
    } catch (restoreError) {
        print("❌ CRITICAL ERROR: Failed to restore backup: " + restoreError);
    }
    quit();
}
print("");

// Step 4: Verify the migration
print("🔍 STEP 4: Verifying migration results");
var finalDocsWithEntityId = db.steps.countDocuments({ "entityId": { $exists: true } });
var finalDocsWithProcessorId = db.steps.countDocuments({ "processorId": { $exists: true } });

print("Documents with 'entityId' field after migration: " + finalDocsWithEntityId);
print("Documents with 'processorId' field after migration: " + finalDocsWithProcessorId);

if (finalDocsWithEntityId === 0 && finalDocsWithProcessorId === docsWithEntityId) {
    print("✅ Migration verification successful!");
} else {
    print("❌ Migration verification failed!");
    print("Expected processorId count: " + docsWithEntityId);
    print("Actual processorId count: " + finalDocsWithProcessorId);
    print("Remaining entityId count: " + finalDocsWithEntityId);
    quit();
}
print("");

// Step 5: Update indexes
print("🔧 STEP 5: Updating database indexes");

// Drop old index if it exists
try {
    db.steps.dropIndex("step_entityid_idx");
    print("✅ Dropped old index: step_entityid_idx");
} catch (error) {
    print("ℹ️  Old index 'step_entityid_idx' not found (this is normal): " + error.message);
}

// Create new index
try {
    db.steps.createIndex({ "processorId": 1 }, { name: "step_processorid_idx" });
    print("✅ Created new index: step_processorid_idx");
} catch (error) {
    print("❌ ERROR creating new index: " + error);
}

// List current indexes for verification
print("");
print("📋 Current indexes on steps collection:");
db.steps.getIndexes().forEach(function(index) {
    print("  - " + index.name + ": " + JSON.stringify(index.key));
});
print("");

// Step 6: Sample data verification
print("🔍 STEP 6: Sample data verification");
var sampleDocs = db.steps.find().limit(3).toArray();
print("Sample documents after migration:");
sampleDocs.forEach(function(doc, index) {
    print("Document " + (index + 1) + ":");
    print("  ID: " + doc.id);
    print("  Version: " + doc.version);
    print("  Name: " + doc.name);
    print("  ProcessorId: " + doc.processorId);
    print("  Has EntityId: " + (doc.entityId !== undefined));
    print("");
});

// Step 7: Migration summary
print("📋 MIGRATION SUMMARY");
print("===================");
print("✅ Migration completed successfully!");
print("📊 Statistics:");
print("  - Total documents processed: " + docsWithEntityId);
print("  - Field 'entityId' renamed to 'processorId': " + renameResult.modifiedCount);
print("  - Old index 'step_entityid_idx' removed");
print("  - New index 'step_processorid_idx' created");
print("  - Backup collection created: steps_backup_entityid_migration");
print("");
print("🔧 Next Steps:");
print("1. Update application code to use 'processorId' instead of 'entityId'");
print("2. Test application functionality thoroughly");
print("3. Remove backup collection after confirming everything works:");
print("   db.steps_backup_entityid_migration.drop()");
print("");
print("⚠️  IMPORTANT: Keep the backup collection until you've verified");
print("   that all application functionality works correctly!");
print("");
print("🎉 Migration completed at: " + new Date());
