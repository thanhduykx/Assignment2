using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccessLayer.Schema;

public static class KnowledgeSqlSchemaInitializer
{
    public static void EnsureTablesCreated(KnowledgeSqlDbContext context)
    {
        if (context.Database.GetService<IRelationalDatabaseCreator>() is not { } creator)
        {
            return;
        }

        if (!creator.Exists())
        {
            creator.Create();
        }

        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            context.Database.Migrate();
        }
        else
        {
            context.Database.EnsureCreated();

            // Apply manual schema patches for new features
            var sqlPatch = @"
                IF COL_LENGTH('rag_subjects', 'IsActive') IS NULL
                BEGIN
                    ALTER TABLE rag_subjects ADD IsActive BIT NOT NULL DEFAULT 1;
                END

                IF OBJECT_ID('rag_subject_students', 'U') IS NULL
                BEGIN
                    CREATE TABLE rag_subject_students (
                        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                        SubjectId UNIQUEIDENTIFIER NOT NULL,
                        UserId UNIQUEIDENTIFIER NOT NULL,
                        CONSTRAINT FK_rag_subject_students_rag_subjects_SubjectId FOREIGN KEY (SubjectId) REFERENCES rag_subjects (Id) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX IX_rag_subject_students_SubjectId_UserId ON rag_subject_students (SubjectId, UserId);
                END
            ";
            context.Database.ExecuteSqlRaw(sqlPatch);
        }
    }
}