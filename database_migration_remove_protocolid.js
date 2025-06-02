// MongoDB Migration Script: Remove ProtocolId from ProcessorEntity
// This script removes the "protocolId" field from the processors collection
// and updates the corresponding index

// Connect to the database
use entitiesmanager;

print("🚀 Starting migration: Remove ProtocolId from ProcessorEntity");
print("Database: " + db.getName());
print("Collection: processors");
print("");

// Colors for output
var RED = '\033[0;31m';
var GREEN = '\033[0;32m';
var YELLOW = '\033[1;33m';
var BLUE = '\033[0;34m';
var NC = '\033[0m'; // No Color

// Step 1: Check current collection state
print("📊 STEP 1: Analyzing current collection state");
var totalDocs = db.processors.countDocuments();
var docsWithProtocolId = db.processors.countDocuments({ "protocolId": { $exists: true } });

print("Total documents in processors collection: " + totalDocs);
print("Documents with 'protocolId' field: " + docsWithProtocolId);
print("");

if (docsWithProtocolId === 0) {
    print("✅ No documents found with 'protocolId' field. Migration may have already been completed.");
    print("Exiting migration script.");
    quit();
}

// Step 2: Create backup collection
print("💾 STEP 2: Creating backup collection");
try {
    db.processors.aggregate([
        { $match: {} }
    ]).forEach(function(doc) {
        db.processors_backup_protocolid_removal.insertOne(doc);
    });
    
    var backupCount = db.processors_backup_protocolid_removal.countDocuments();
    print("✅ Backup created successfully: " + backupCount + " documents backed up");
    print("Backup collection: processors_backup_protocolid_removal");
} catch (error) {
    print("❌ ERROR creating backup: " + error);
    quit();
}
print("");

// Step 3: Remove protocolId field from all documents
print("🗑️ STEP 3: Removing 'protocolId' field from all documents");
try {
    var removeResult = db.processors.updateMany(
        { "protocolId": { $exists: true } },
        { $unset: { "protocolId": "" } }
    );
    
    print("✅ Field removal completed successfully");
    print("Documents modified: " + removeResult.modifiedCount);
    print("Documents matched: " + removeResult.matchedCount);
} catch (error) {
    print("❌ ERROR during field removal: " + error);
    print("Attempting to restore from backup...");
    
    // Restore from backup
    try {
        db.processors.deleteMany({});
        db.processors_backup_protocolid_removal.find().forEach(function(doc) {
            delete doc._id; // Remove _id to avoid conflicts
            db.processors.insertOne(doc);
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
var finalDocsWithProtocolId = db.processors.countDocuments({ "protocolId": { $exists: true } });
var finalTotalDocs = db.processors.countDocuments();

print("Documents with 'protocolId' field after migration: " + finalDocsWithProtocolId);
print("Total documents after migration: " + finalTotalDocs);

if (finalDocsWithProtocolId === 0 && finalTotalDocs === totalDocs) {
    print("✅ Migration verification successful!");
} else {
    print("❌ Migration verification failed!");
    print("Expected total documents: " + totalDocs);
    print("Actual total documents: " + finalTotalDocs);
    print("Remaining protocolId count: " + finalDocsWithProtocolId);
    quit();
}
print("");

// Step 5: Update indexes
print("🔧 STEP 5: Updating database indexes");

// Drop ProtocolId index if it exists
try {
    var indexes = db.processors.getIndexes();
    var protocolIdIndexExists = false;
    
    indexes.forEach(function(index) {
        if (index.key && index.key.protocolId) {
            protocolIdIndexExists = true;
            print("Found ProtocolId index: " + index.name);
        }
    });
    
    if (protocolIdIndexExists) {
        // Try to drop by field (MongoDB will find the index)
        db.processors.dropIndex({ "protocolId": 1 });
        print("✅ Dropped ProtocolId index");
    } else {
        print("ℹ️  No ProtocolId index found (this is normal if index was not created)");
    }
} catch (error) {
    print("ℹ️  Error dropping ProtocolId index (may not exist): " + error.message);
}

// List current indexes for verification
print("");
print("📋 Current indexes on processors collection:");
db.processors.getIndexes().forEach(function(index) {
    print("  - " + index.name + ": " + JSON.stringify(index.key));
});
print("");

// Step 6: Sample data verification
print("🔍 STEP 6: Sample data verification");
var sampleDocs = db.processors.find().limit(3).toArray();
print("Sample documents after migration:");
sampleDocs.forEach(function(doc, index) {
    print("Document " + (index + 1) + ":");
    print("  ID: " + doc.id);
    print("  Version: " + doc.version);
    print("  Name: " + doc.name);
    print("  InputSchemaId: " + doc.inputSchemaId);
    print("  OutputSchemaId: " + doc.outputSchemaId);
    print("  Has ProtocolId: " + (doc.protocolId !== undefined));
    print("");
});

// Step 7: Migration summary
print("📋 MIGRATION SUMMARY");
print("===================");
print("✅ Migration completed successfully!");
print("📊 Statistics:");
print("  - Total documents processed: " + docsWithProtocolId);
print("  - Field 'protocolId' removed from: " + removeResult.modifiedCount + " documents");
print("  - ProtocolId index removed (if existed)");
print("  - Backup collection created: processors_backup_protocolid_removal");
print("");
print("🔧 Next Steps:");
print("1. Update application code to remove ProtocolId references");
print("2. Test application functionality thoroughly");
print("3. Remove backup collection after confirming everything works:");
print("   db.processors_backup_protocolid_removal.drop()");
print("");
print("⚠️  IMPORTANT: Keep the backup collection until you've verified");
print("   that all application functionality works correctly!");
print("");
print("🎉 Migration completed at: " + new Date());

// Step 8: Breaking changes warning
print("");
print("⚠️  BREAKING CHANGES WARNING");
print("============================");
print("This migration removes the ProtocolId field from ProcessorEntity.");
print("This is a BREAKING CHANGE that affects:");
print("1. API consumers expecting ProtocolId in JSON responses");
print("2. MassTransit message consumers expecting ProtocolId in events");
print("3. BaseProcessor framework applications using ProtocolId");
print("");
print("Ensure all dependent systems are updated before deploying this change!");
print("");
print("🔄 ROLLBACK PROCEDURE:");
print("If rollback is needed, restore from backup:");
print("  db.processors.deleteMany({});");
print("  db.processors_backup_protocolid_removal.find().forEach(function(doc) {");
print("      delete doc._id;");
print("      db.processors.insertOne(doc);");
print("  });");
