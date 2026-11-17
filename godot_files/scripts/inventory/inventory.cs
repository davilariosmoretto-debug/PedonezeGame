using System;
using Godot;
using System.Collections.Generic;
using MainCharacterFile;
using SimpleItems;
using AllyFile;
using System.IO;
using Tomlyn;

namespace InventoryFile {
	class Inventory {
		public List<Item> StoredItems { get; set; }
        public List<MainCharacter> TeamStatus { get; set; }
        public Dictionary<string, List<string>> PlayerSpellSet { get; set; }

        private static readonly string InventoryBaseFile =
            ProjectSettings.GlobalizePath("res://scripts/inventory/inventory.toml");

        internal static readonly TomlSerializerOptions TomlOptions = new() {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(Item)] = [
                        new(typeof(Weapon), "Weapon"),
                        new(typeof(Shield), "Shield"),
                        new(typeof(Armour), "Armour")
                    ],
                    [typeof(MainCharacter)] = [
                        new(typeof(Archer), "Archer"),
                        new(typeof(Wizard), "Wizard"),
                        new(typeof(Cleric), "Cleric"),
                        new(typeof(Gladiator), "Gladiator")
                    ]
                }
            }
	    };


        public static Inventory InitializeTOML(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

            if (!File.Exists(path) && File.Exists(InventoryBaseFile)) {
                File.Copy(InventoryBaseFile, path);
            }

            #nullable enable
                Inventory? inv = null;
            #nullable disable

            if (File.Exists(path)) {
                string toml = File.ReadAllText(path);

                if (!string.IsNullOrWhiteSpace(toml)) {
                    inv = TomlSerializer.Deserialize<Inventory>(toml, TomlOptions);
                }
            }

            inv ??= new Inventory();

            inv.StoredItems ??= new List<Item>();
            inv.TeamStatus ??= new List<MainCharacter>();
            inv.PlayerSpellSet ??= new Dictionary<string, List<string>>();

            foreach (string key in new[] { "Archer", "Wizard", "Cleric", "Gladiator" }) {
                inv.PlayerSpellSet.TryAdd(key, new List<string>());
            }

            inv.UpdateTOML(path);
            return inv;
        }


        public static Inventory LoadTOML(string path) {
            try
            {
                if (!File.Exists(path)) {
                    throw new FileNotFoundException(
                        $"Inventory file was not found: {Path.GetFullPath(path)}",
                        path
                    );
                }
                
                Inventory inv;
                string toml = File.ReadAllText(path);

                if (toml.Length != 0) { 
                    inv = TomlSerializer.Deserialize<Inventory>(toml, TomlOptions);
                } else {
                    inv = new();
                }


                if (inv == null) {
                    throw new InvalidDataException(
                        $"Deserializing the inventory returned null. File: {Path.GetFullPath(path)}"
                    );
                }

                return inv;
            }
            catch (Exception e) {
                // Log the full exception in Godot, including its stack trace.
                Godot.GD.PrintErr($"Failed to load inventory from '{path}':\n{e}");
                throw; // Re-throw so the caller doesn't continue with an invalid inventory.
            }
        }


		public void UpdateTOML(string path) {
		   string toml = TomlSerializer.Serialize(this, TomlOptions);
		   // Console.WriteLine($"toml to write: {toml}");
		   File.WriteAllText(path, toml);
		}

	}
}



