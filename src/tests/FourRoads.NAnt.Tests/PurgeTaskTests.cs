using System;
using System.IO;
using System.Xml;
using FourRoads.NAnt.Tasks;
using NAnt.Core;
using NUnit.Framework;

namespace FourRoads.NAnt.Tests
{
	[TestFixture]
	public class PurgeTaskTests
	{
		private const string PURGE_DIRECTORY = "PurgeDir";
		private PurgeTask _task;

		/// <summary>
		/// Tests PurgeTask with no files being kept in purge folder
		/// </summary>
		[Test]
		public void PurgeWithNoneKeptTest()
		{
			_task = new PurgeTask {FilesToKeep = 0};
			ExecuteTest(_task, "PurgeTaskTest");
		}

		/// <summary>
		/// Tests PurgeTask with only one file being kept in purge folder
		/// </summary>
		[Test]
		public void PurgeWithOneKeptTest()
		{
			_task = new PurgeTask { FilesToKeep = 1 };
			ExecuteTest(_task, "PurgeTaskTest");
		}

		/// <summary>
		/// Tests PurgeTask with three files being kept in purge folder
		/// </summary>
		[Test]
		public void PurgeWithThreeKeptTest()
		{
			_task = new PurgeTask { FilesToKeep = 3 };
			ExecuteTest(_task, "PurgeTaskTest");
		}

		/// <summary>
		/// Tests PurgeTask with all files being kept in purge folder
		/// </summary>
		[Test]
		public void PurgeWithAllKeptTest()
		{
			_task = new PurgeTask { FilesToKeep = int.MaxValue };
			ExecuteTest(_task, "PurgeTaskTest");
		}

		/// <summary>
		/// Tests PurgeTask with test mode turned on
		/// </summary>
		[Test]
		public void PurgeInTestModeTest()
		{
			_task = new PurgeTask { TestMode = true };
			ExecuteTest(_task, "PurgeTaskTest");
		}

		/// <summary>
		/// Creates test directories
		/// </summary>
		[SetUp]
		public void Init()
		{
			// Populate purge directory
			Directory.CreateDirectory(PURGE_DIRECTORY);
		}

		/// <summary>
		/// Delete purge directory
		/// </summary>
		[TearDown]
		public void CleanUp()
		{
			if (Directory.Exists(PURGE_DIRECTORY))
				Directory.Delete(PURGE_DIRECTORY, true);
		}

		private static void LoadTree(string treeName)
		{
			XmlDocument doc = new XmlDocument();

			doc.LoadXml(Properties.Resources.trees);

			XmlNode tree = doc.SelectSingleNode(string.Format("/trees/tree[@name='{0}']", treeName));

			if (tree == null)
				return;

			XmlNode node = tree.SelectSingleNode("source");

			LoadDirectories(node, new DirectoryInfo(PURGE_DIRECTORY));
		}

		private static void LoadDirectories(XmlNode node, FileSystemInfo directory)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				switch (child.Name.ToLower())
				{
					case "file":
						File.Create(String.Format(@"{0}\{1}", directory.FullName, child.Attributes["name"].Value));
						break;
					case "directory":
						{
							DirectoryInfo subDirectory =
								Directory.CreateDirectory(String.Format(@"{0}\{1}", directory.FullName, child.Attributes["name"].Value));

							LoadDirectories(child, subDirectory);
						}
						break;
				}
			}
		}

		private static void ExecuteTest(PurgeTask task, string treeName)
		{
			LoadTree(treeName);

			DirectoryInfo toDirectory = new DirectoryInfo(PURGE_DIRECTORY);
			XmlDocument doc = new XmlDocument();
			int children = toDirectory.GetFiles().Length;
			int expected = task.FilesToKeep > children ? children : task.FilesToKeep;

			doc.LoadXml(Properties.Resources.build);
			task.Project = new Project(doc, Level.Info, 0);
			task.Directory = toDirectory;
			task.Execute();

			Assert.AreEqual(expected, toDirectory.GetFiles().Length);
		}
	}
}
