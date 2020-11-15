using System;
using System.Diagnostics;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationVisitor
    {
        void AddUrlRedirect(string source, string destination);
        MigratedData GetMigratedData(string objectType , string sourceKey);
        void CreateLogEntry(string message, EventLogEntryType information);
        User GetUserOrFormerMember(int? userId);
        void EnsureGroupMember(Group @group, User author);
        void EnsureBlogAuthor(Blog blog ,User user);
        void SafeRunAs(string userName, Action action);
        void EnsureUploadPermissions(Gallery gallery);
    }
}