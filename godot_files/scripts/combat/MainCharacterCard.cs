using Godot;
using MainCharacterFile;
using EventSystem;

using static EventSystem.EventHandler;
using static EventSystem.EventBody;

namespace CardFile {
    public partial class MainCharacterCard : Card {
        public Label ManaLabel { get; private set; }
        public Label DamageLabel { get; private set; }
        public AnimationPlayer Anim { get; private set; }
        public Panel CharacterFacePanel { get; private set; }
        private Image characterFaceImage;

        public override void _Ready() {
            NameLabel          = GetNode<Label>("statsPanel/labelHolder/name");
            HealthLabel        = GetNode<Label>("statsPanel/values/hpValue");
            ManaLabel          = GetNode<Label>("statsPanel/values/manaValue");

            Anim               = GetNode<AnimationPlayer>("AnimationPlayer");
            DamageLabel        = GetNode<Label>("DamageLabel");
            CharacterFacePanel = GetNode<Panel>("head");
        }

        public  void UpdateValues(MainCharacter mainCharacter) {
                string characterFacePath;
                if (mainCharacter.Health > 0) {
                    characterFacePath = mainCharacter switch {
                        AllyFile.Gladiator _ => "res://assets/char/player_team/InBattle/FaceGladiator.png",
                        AllyFile.Cleric _    => "res://assets/char/player_team/InBattle/FaceCleric.png",
                        AllyFile.Archer _    => "res://assets/char/player_team/InBattle/FaceArcher.png",
                        AllyFile.Wizard _    => "res://assets/char/player_team/InBattle/FaceWizard.png",
                        _                    => ""
                    };
                } else {
                    characterFacePath         = mainCharacter switch {
                        AllyFile.Gladiator _ => "res://assets/char/player_team/InBattle/Dead/FaceGladiator.png",
                        AllyFile.Cleric _    => "res://assets/char/player_team/InBattle/Dead/FaceCleric.png",
                        AllyFile.Archer _    => "res://assets/char/player_team/InBattle/Dead/FaceArcher.png",
                        AllyFile.Wizard _    => "res://assets/char/player_team/InBattle/Dead/FaceWizard.png",
                        _ => ""
                    };
                }

                characterFaceImage = Image.LoadFromFile(characterFacePath);

                ThrowEvent(new Event("Sets The Label <Name>", EventKind.Label,
                    Label(NameLabel,$"{mainCharacter.Name}")));
                GD.Print($"Threw set label mana for {mainCharacter.Name}");
                ThrowEvent(new Event("Sets The Label <Mana>", EventKind.Label,
                    Label(ManaLabel,$"{mainCharacter.Mana}/{mainCharacter.manaMax}")));
                GD.Print($"Threw set label mana for {mainCharacter.Name}");
                ThrowEvent(new Event("Sets The Label <Health>", EventKind.Label,
                    Label(HealthLabel,$"{mainCharacter.Health}/{mainCharacter.healthMax}")));
                GD.Print($"Threw set label health for {mainCharacter.Name}");
                ThrowEvent(new Event("Sets The Image <CharacterFace>", EventKind.ImageUpdate,
                    ImageUpdate(CharacterFacePanel, characterFaceImage)));
                GD.Print($"Threw set image for {mainCharacter.Name}");
        }
    }
}

