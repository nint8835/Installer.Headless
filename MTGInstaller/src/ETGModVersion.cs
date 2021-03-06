﻿using System;
using YamlDotNet.Serialization;

namespace MTGInstaller {
	public class ETGModVersion {
		[YamlMember(Alias = "key")]
		public string Key { set; get; }

		[YamlMember(Alias = "name")]
		public string DisplayName { set; get; }

		[YamlMember(Alias = "path")]
		public string Path { set; get; }

		[YamlMember(Alias = "url")]
		public string URL { set; get; }

		[YamlMember(Alias = "release_date")]
		public string ReleaseDate { set; get; }

		[YamlMember(Alias = "beta")]
		public bool Beta { set; get; } = false;

		[YamlMember(Alias = "supported_gungeon")]
		public string SupportedGungeon { set; get; } = null;

		[YamlMember(Alias = "requires_patched_exe")]
		public bool RequiresPatchedExe { set; get; } = false;

		public override string ToString() {
			if (Beta) return $"[{Key} β] {DisplayName} ({ReleaseDate ?? "N/A"})";
			return $"[{Key} R] {DisplayName} ({ReleaseDate ?? "N/A"})";
		}
	}
}
