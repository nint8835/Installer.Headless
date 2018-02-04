﻿using System;
using System.Collections.Generic;
using System.IO;
using MTGInstaller.YAML;

namespace MTGInstaller {
	public class InstallerFrontend {
		[Flags]
		public enum InstallerOptions {
			None = 0,
			SkipVersionChecks = 1,
			ForceBackup = 2,
			HTTP = 4,
			LeavePatchDLLs = 8
		}

		public struct ComponentInfo {
			public ComponentInfo(string name, string version) {
				Name = name;
				Version = version;
			}

			public string Name;
			public string Version;
		}

		private Installer _Installer;
		private Downloader _Downloader;

		public InstallerOptions Options;
		public List<ETGModComponent> Components;

		public Dictionary<string, ETGModComponent> AvailableComponents {
			get { return _Downloader.Components; }
		}

		public class InstallationFailedException : Exception { public InstallationFailedException(string msg) : base(msg) {} }

		public InstallerFrontend(InstallerOptions options = InstallerOptions.None) {
			Options = options;
			_Downloader = new Downloader(force_http: Options.HasFlag(InstallerOptions.HTTP));
			_Installer = new Installer(_Downloader, exe_path: null);
		}

		public void LoadComponentsFile(string path) {
			_Downloader.AddComponentsFile(File.ReadAllText(path));
		}

		public ETGModComponent TryGetComponent(string name) {
			ETGModComponent component = null;
			AvailableComponents.TryGetValue(name, out component);
			return component;
		}

		public DownloadedBuild Download(ComponentInfo component_info, bool force = false) {
			var component = TryGetComponent(component_info.Name);
			if (component != null) {
				ETGModVersion version = null;

				if (component_info.Version != null) {
					foreach (var ver in component.Versions) {
						if (ver.Key == component_info.Version) {
							version = ver;
							break;
						}
					}
					if (version == null) {
						throw new InstallationFailedException($"Version {component_info.Version} of component {component.Name} doesn't exist.");
					}
				} else version = component.Versions[0];

				var dest = version.DisplayName;
				if (Directory.Exists(dest)) {
					if (force) {
						Directory.Delete(dest, recursive: true);
					} else {
						throw new InstallationFailedException($"Version is already downloaded in the '{dest}' folder (use --force to redownload)");
					}
				}

				try {
					return _Downloader.Download(version, dest);
				} catch (System.Net.WebException e) {
					var resp = e.Response as System.Net.HttpWebResponse;
					if (resp?.StatusCode == System.Net.HttpStatusCode.NotFound) {
						throw new InstallationFailedException($"Error 404 while downloading version {version.DisplayName} of component {component.Name}.");
					} else {
						throw new InstallationFailedException($"Unhandled error occured while downloading version {version.DisplayName} of component {component.Name}: {e.Message}.");
					}
				}
			}
			throw new InstallationFailedException($"Component {component_info.Name} doesn't exist.");
		}

		public void Install(IEnumerable<ComponentInfo> components, string exe_path = null) {
			var real_components = new List<ComponentVersion>();
			var used_components = new HashSet<ETGModComponent>();
			var gungeon_version = Autodetector.GetVersionIn(exe_path);

			foreach (var com in components) {
				ETGModComponent component = _Downloader.TryGet(com.Name.Trim());

				if (component != null) {
					if (used_components.Contains(component)) {
						throw new InstallationFailedException($"Duplicate {component.Name} component.");
					}
					used_components.Add(component);

					ETGModVersion version = null;
					if (com.Version == null) version = component.Versions[0];
					else {
						foreach (var ver in component.Versions) {
							if (ver.Key == com.Version.Trim()) {
								version = ver;
								break;
							}
						}
						if (version == null) {
							throw new InstallationFailedException($"Version {com.Version} of component {com.Name} doesn't exist.");
						}
					}

					real_components.Add(new ComponentVersion(component, version));
				} else {
					throw new InstallationFailedException($"Component {com.Name} doesn't exist in the list of components.");
				}
			}

			Install(real_components, exe_path);
		}
		

		public void Install(IEnumerable<ComponentVersion> components, string exe_path = null) {
			_Installer.ChangeExePath(exe_path);

			var used_components = new HashSet<ETGModComponent>();
			var gungeon_version = Autodetector.GetVersionIn(exe_path);

			if (!Options.HasFlag(InstallerOptions.ForceBackup)) _Installer.Restore();
			_Installer.Backup(Options.HasFlag(InstallerOptions.ForceBackup));

			foreach (var pair in components) {
				using (var build = _Downloader.Download(pair.Version)) {
					var installable = new Installer.InstallableComponent(pair.Component, pair.Version, build);
					if (!Options.HasFlag(InstallerOptions.SkipVersionChecks)) {
						try {
							installable.ValidateGungeonVersion(gungeon_version);
						} catch (Installer.InstallableComponent.VersionMismatchException e) {
							throw new InstallationFailedException(e.Message);
						}
					}

					_Installer.InstallComponent(installable, Options.HasFlag(InstallerOptions.LeavePatchDLLs));
				}
			}
		}
	}
}