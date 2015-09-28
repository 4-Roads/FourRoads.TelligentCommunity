using System;
using System.IO;
using System.Xml;
using NAnt.Core;
using NUnit.Framework;
using FourRoads.NAnt.Tasks;

namespace FourRoads.NAnt.Tests
{
	/// <summary>
	/// Contains unit tests for the SyncTask class
	/// </summary>
	[TestFixture]
	public class SyncTaskTests
	{
		private readonly string _todirectory = "ToDir";
		private readonly string _sourcedirectory = "SourceDir";

		/// <summary>
		/// Tests SyncTask with only copies required from source folder to destination folder
		/// </summary>
		[Test]
		public void ExecuteTaskWithCopiesTest()
		{
			ExecuteTest("ExecuteTaskWithCopiesTest");
		}

		/// <summary>
		/// Tests SyncTask with only deletions required from destination folder
		/// </summary>
		[Test]
		public void ExecuteTaskWithDeletionsTest()
		{
			ExecuteTest("ExecuteTaskWithDeletionsTest");
		}

		/// <summary>
		/// Tests SyncTask with copies and deletes required
		/// </summary>
		[Test]
		public void ExecuteTaskWithDeletionsAndCopiesTest()
		{
			ExecuteTest("ExecuteTaskWithDeletionsAndCopiesTest");
		}

		/// <summary>
		/// Creates test directories
		/// </summary>
		[SetUp]
		public void Init()
		{
			// Populate ToDir
			Directory.CreateDirectory(_todirectory);

			// Populate SourceDir
			Directory.CreateDirectory(_sourcedirectory);
		}

		/// <summary>
		/// Deletes any remaining test directories
		/// </summary>
		[TearDown]
		public void CleanUp()
		{
			if(Directory.Exists(_todirectory))
				Directory.Delete(_todirectory, true);

			if(Directory.Exists(_sourcedirectory))
				Directory.Delete(_sourcedirectory, true);
		}

		private static int GetDescendantCount(DirectoryInfo dir)
		{
			int count = dir.GetFileSystemInfos().Length;

			foreach(DirectoryInfo subDir in dir.GetDirectories())
			{
				count += GetDescendantCount(subDir);
			}

			return count;
		}

		private void LoadTree(string treeName)
		{
			XmlDocument doc = new XmlDocument();
			XmlNode tree;

			doc.LoadXml(Properties.Resources.trees);
			tree = doc.SelectSingleNode(string.Format("/trees/tree[@name='{0}']", treeName));

			if (tree != null)
			{
				XmlNode node;

				node = tree.SelectSingleNode("source");
				LoadDirectories(node, new DirectoryInfo(_sourcedirectory));

				node = tree.SelectSingleNode("destination");
				LoadDirectories(node, new DirectoryInfo(_todirectory));
			}
		}

		private static void LoadDirectories(XmlNode node, FileSystemInfo directory)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name.ToLower() == "file")
				{
					File.Create(String.Format(@"{0}\{1}", directory.FullName, child.Attributes["name"].Value));
				}
				if (child.Name.ToLower() == "directory")
				{
					DirectoryInfo subDirectory = Directory.CreateDirectory(String.Format(@"{0}\{1}", directory.FullName, child.Attributes["name"].Value));

					LoadDirectories(child, subDirectory);
				}
			}
		}

		private void ExecuteTest(string treeName)
		{
			SyncTask task = new SyncTask();
			DirectoryInfo toDirectory = new DirectoryInfo(_todirectory);
			DirectoryInfo sourceDirectory = new DirectoryInfo(_sourcedirectory);
			XmlDocument doc = new XmlDocument();
			int expected;

			LoadTree(treeName);
			expected = GetDescendantCount(sourceDirectory);
			doc.LoadXml(Properties.Resources.build);
			task.Project = new Project(doc, Level.Info, 0);
			task.ToDirectory = toDirectory;
			task.SourceDirectory = sourceDirectory;
			task.Execute();

			Assert.AreEqual(expected, GetDescendantCount(sourceDirectory));
			Assert.AreEqual(expected, GetDescendantCount(toDirectory));
		}
	}
}
