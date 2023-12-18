using System;
using System.IO;
using System.Timers;
using System.Security.Cryptography;
using System.Collections;

class FolderSynchronization
{
    static string? sourceFolder;
    static string? replicaFolder;
    static string? logFilePath;
    static int syncInterval;

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSynchronization <sourceFolder> <replicaFolder> <syncIntervalInSeconds> <logFilePath>");
            return;
        }

        sourceFolder = args[0];
        replicaFolder = args[1];

        if (!Directory.Exists(sourceFolder) || !Directory.Exists(replicaFolder))
        {
            Console.WriteLine("Source or replica folder does not exist.");
            return;
        }

        if (!int.TryParse(args[2], out syncInterval) || syncInterval <= 0)
        {
            Console.WriteLine("Invalid sync interval.");
            return;
        }

        logFilePath = args[3];
        using StreamWriter writer = new(logFilePath);
        // Initial synchronization
        SynchronizeFolders(args[0], args[1], writer);

        // Set up periodic synchronization
        System.Timers.Timer timer = new(syncInterval * 1000);
        timer.Elapsed += (sender, e) => SynchronizeFolders(args[0], args[1], writer);
        timer.Start();

        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    static void SynchronizeFolders(string sourceDir, string replicaDir, StreamWriter writer)
    {
        // Get the list of subdirectories in the source and replica directories
        string[] sourceSubdirectories = Directory.GetDirectories(sourceDir);
        string[] replicaSubdirectories = Directory.GetDirectories(replicaDir);

        // Directories to be removed in the replica directory
        var removedDirectories = replicaSubdirectories.Select(dirPath => Path.GetFileName(dirPath))
                                                 .Except(sourceSubdirectories.Select(dirPath => Path.GetFileName(dirPath)))
                                                 .ToList();

        foreach (var directory in removedDirectories)
        {
            // Perform action for removed directory (e.g., delete from replica)
            Directory.Delete(directory, true); // Recursive delete
            LogToFile($"Directory removed: {directory}", writer);

        }

        // Synchronize files in the current directory
        SynchronizeFiles(sourceDir, replicaDir, writer);

        // Synchronize subdirectories
        foreach (var sourceSubdirectory in sourceSubdirectories)
        {
            string subdirectoryName = Path.GetFileName(sourceSubdirectory);
            string replicaSubdirectory = Path.Combine(replicaDir, subdirectoryName);

            if (!Directory.Exists(replicaSubdirectory))
            {
                Directory.CreateDirectory(replicaSubdirectory);
                LogToFile($"Created new Subdirectory {subdirectoryName}", writer);
            }

            // Recursively synchronize subdirectories
            SynchronizeFolders(sourceSubdirectory, replicaSubdirectory, writer);
        }
    }

    static void SynchronizeFiles(string sourceDir, string replicaDir, StreamWriter writer)
    {
        string[] sourceFiles = Directory.GetFiles(sourceDir);
        string[] replicaFiles = Directory.GetFiles(replicaDir);

        var removedFiles = replicaFiles.Select(filePath => Path.GetFileName(filePath))
                                   .Except(sourceFiles.Select(filePath => Path.GetFileName(filePath)))
                                   .ToList();

        foreach (var file in removedFiles)
        {
            string filePath = Path.Combine(replicaDir, file);
            File.Delete(filePath);
            LogToFile($"File removed from replica: {file}", writer);
        }

        foreach (var sourceFile in sourceFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            string replicaFilePath = Path.Combine(replicaDir, fileName);

            if (!File.Exists(replicaFilePath)) 
            {
                // Create new file in replica
                File.Copy(sourceFile, replicaFilePath, true);
                LogToFile($"Created new file {sourceFile} to {replicaFilePath}", writer);
            }
                
            if (!AreFilesEqual(sourceFile, replicaFilePath))
            {
                // Copy the file from source to replica
                File.Copy(sourceFile, replicaFilePath, true);
                LogToFile($"Copied file {sourceFile} to {replicaFilePath}", writer);
            }
        }
    }

    static bool AreFilesEqual(string filePath1, string filePath2)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream1 = File.OpenRead(filePath1))
            using (var stream2 = File.OpenRead(filePath2)) 
            {
                byte[] hash1 = md5.ComputeHash(stream1);
                byte[] hash2 = md5.ComputeHash(stream2);

                return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
            }
        }
    }

    static void LogToFile(string message, StreamWriter writer)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {message}";

        // Write the log message to the file
        writer.WriteLine(logMessage);

        // Ensure that the log is written on runtime
        writer.Flush();

        // Also print the log message to the console
        Console.WriteLine(logMessage);
    }
}
