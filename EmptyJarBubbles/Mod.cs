﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace EmptyJarBubbles;

internal class Mod: StardewModdingAPI.Mod {
    internal static Configuration Config;
    internal static int CurrentEmoteFrame;
    internal static int CurrentEmoteInterval;
    internal static List<Object> Machines;
    internal static Dictionary<string, MachineData> MachineData;

    public override void Entry(IModHelper helper) {
        Config = Helper.ReadConfig<Configuration>();
        I18n.Init(helper.Translation);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += SaveLoaded;
        helper.Events.GameLoop.DayStarted += DayStarted;
        helper.Events.GameLoop.ReturnedToTitle += ReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += UpdateTicked;
        helper.Events.World.ObjectListChanged += ObjectListChanged;
        helper.Events.Player.Warped += Warped;
        helper.Events.Display.MenuChanged += MenuChanged;
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is not null) RegisterConfig(configMenu);
    }
    
    private void UpdateTicked(object sender, UpdateTickedEventArgs e) {
        if (!Config.Enabled) return;
        AnimateEmote();
    }

    private static void AnimateEmote() {
        CurrentEmoteInterval += Game1.currentGameTime.ElapsedGameTime.Milliseconds;

        if (CurrentEmoteFrame is < 16 or > 19) CurrentEmoteFrame = 16;
        if (CurrentEmoteInterval > Config.EmoteInterval) {
            if (CurrentEmoteFrame < 19) CurrentEmoteFrame++;
            else CurrentEmoteFrame = 16;
            CurrentEmoteInterval = 0;
        }
    }

    private void SaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        Helper.Events.Display.RenderedWorld += RenderBubbles;
        Machines = new();
        MachineData = DataLoader.Machines(Game1.content);
    }

    private void ReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        Helper.Events.Display.RenderedWorld -= RenderBubbles;
    }
    
    private void Warped(object sender, WarpedEventArgs e) {
        BuildMachineList();
    }
    
    private void DayStarted(object sender, DayStartedEventArgs e) {
        BuildMachineList();
    }

    private void MenuChanged(object sender, MenuChangedEventArgs e) {
        BuildMachineList();
    }

    private void ObjectListChanged(object sender, ObjectListChangedEventArgs e) {
        if (!Config.Enabled) return;
        
        var removedMachines = e.Removed
            .Where(kvp => IsValidMachine(kvp.Value))
            .Select(kvp => kvp.Value)
            .ToList();

        var newMachines = e.Added
            .Where(kvp => IsValidMachine(kvp.Value))
            .Select(kvp => kvp.Value)
            .ToList();

        Machines.RemoveAll(x => removedMachines.Contains(x));
        Machines.AddRange(newMachines);
    }
    
    private void BuildMachineList()
    {
        if (!Config.Enabled) return;
        if (Game1.currentLocation is null) return;

        Machines = Game1.currentLocation.Objects.Values
            .Where(IsValidMachine)
            .ToList();
        
        // Machines = Game1.currentLocation.Objects.Values
        //     .Where(o => MachineData.ContainsKey(o.QualifiedItemId))
        //     .ToList();
    }

    private bool IsValidMachine(Object o) {
        return IsObjectJar(o) || IsObjectKeg(o) || IsObjectCask(o) ||
               IsObjectMayonnaiseMachine(o) || IsObjectCheesePress(o) || IsObjectLoom(o) ||
               IsObjectOilMaker(o) || IsObjectDehydrator(o) || IsObjectFishSmoker(o) ||
               IsObjectBaitMaker(o) || IsObjectBoneMill(o) || IsObjectCharcoalKiln(o) ||
               IsObjectCrystalarium(o) || IsObjectFurnace(o) || IsObjectRecyclingMachine(o) ||
               IsObjectSeedMaker(o) || IsObjectSlimeEggPress(o) || IsObjectCrabPot(o) ||
               IsObjectDeconstructor(o);
    }

    private void RenderBubbles(object sender, RenderedWorldEventArgs e) {
        if (!Config.Enabled) return;
        
        var readyMachines = Machines.Where(o => 
                // MinutesUntilReady <= 0 because casks that have an item removed will be < 0
                (o is not CrabPot && o.MinutesUntilReady <= 0 && !o.readyForHarvest.Value) ||
                (o is CrabPot pot && pot.bait.Value is null && pot.heldObject.Value is null))
            .ToList();
        
        foreach (var machine in readyMachines) DrawBubbles(machine, e.SpriteBatch);
    }
    
    private void DrawBubbles(Object o, SpriteBatch spriteBatch) {
        Vector2 tilePosition = o.TileLocation * 64;
        Vector2 emotePosition = Game1.GlobalToLocal(tilePosition);
        emotePosition += new Vector2((100 - Config.SizePercent) / 100f * 32, -Config.OffsetY);
        if (o is CrabPot pot) {
            emotePosition += pot.directionOffset.Value;
            emotePosition.Y += pot.yBob + 20;
        }
        
        spriteBatch.Draw(Game1.emoteSpriteSheet,
            emotePosition, 
            new Rectangle(CurrentEmoteFrame * 16 % Game1.emoteSpriteSheet.Width, CurrentEmoteFrame * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16),
            Color.White * (Config.OpacityPercent / 100f), 
            0f,
            Vector2.Zero, 
            4f * Config.SizePercent / 100f, 
            SpriteEffects.None, 
            (tilePosition.Y + 37) / 10000f);
    }

    #region IsObjectMethods
    private bool IsObjectJar(Object o) {
        if (!Config.JarsEnabled) return false;
        return o.QualifiedItemId == "(BC)15";
    }

    private bool IsObjectKeg(Object o) {
        if (!Config.KegsEnabled) return false;
        return o.QualifiedItemId == "(BC)12";
    }
    
    private bool IsObjectCask(Object o) {
        if (!Config.CasksEnabled) return false;
        return o.QualifiedItemId == "(BC)163";
    }
    
    private bool IsObjectMayonnaiseMachine(Object o) {
        if (!Config.MayonnaiseMachinesEnabled) return false;
        return o.QualifiedItemId == "(BC)24";
    }
    
    private bool IsObjectCheesePress(Object o) {
        if (!Config.CheesePressesEnabled) return false;
        return o.QualifiedItemId == "(BC)16";
    }
    
    private bool IsObjectLoom(Object o) {
        if (!Config.LoomsEnabled) return false;
        return o.QualifiedItemId == "(BC)17";
    }
    
    private bool IsObjectOilMaker(Object o) {
        if (!Config.OilMakersEnabled) return false;
        return o.QualifiedItemId == "(BC)19";
    }
    
    private bool IsObjectDehydrator(Object o) {
        if (!Config.DehydratorsEnabled) return false;
        return o.QualifiedItemId == "(BC)Dehydrator";
    }
    
    private bool IsObjectFishSmoker(Object o) {
        if (!Config.FishSmokersEnabled) return false;
        return o.QualifiedItemId == "(BC)FishSmoker";
    }
    
    private bool IsObjectBaitMaker(Object o) {
        if (!Config.BaitMakersEnabled) return false;
        return o.QualifiedItemId == "(BC)BaitMaker";
    }
    
    private bool IsObjectBoneMill(Object o) {
        if (!Config.BoneMillsEnabled) return false;
        return o.QualifiedItemId == "(BC)90";
    }
    
    private bool IsObjectCharcoalKiln(Object o) {
        if (!Config.CharcoalKilnsEnabled) return false;
        return o.QualifiedItemId == "(BC)114";
    }
    
    private bool IsObjectCrystalarium(Object o) {
        if (!Config.CrystalariumsEnabled) return false;
        return o.QualifiedItemId == "(BC)21";
    }
    
    private bool IsObjectFurnace(Object o) {
        if (!Config.FurnacesEnabled) return false;
        return o.QualifiedItemId is "(BC)13" or "(BC)HeavyFurnace";
    }
    
    private bool IsObjectRecyclingMachine(Object o) {
        if (!Config.RecyclingMachinesEnabled) return false;
        return o.QualifiedItemId == "(BC)20";
    }
    
    private bool IsObjectSeedMaker(Object o) {
        if (!Config.SeedMakersEnabled) return false;
        return o.QualifiedItemId == "(BC)25";
    }
    
    private bool IsObjectSlimeEggPress(Object o) {
        if (!Config.SlimeEggPressesEnabled) return false;
        return o.QualifiedItemId == "(BC)158";
    }
    
    private bool IsObjectCrabPot(Object o) {
        if (!Config.CrabPotsEnabled) return false;
        return o.QualifiedItemId == "(O)710";
    }
    
    private bool IsObjectDeconstructor(Object o) {
        if (!Config.DeconstructorsEnabled) return false;
        return o.QualifiedItemId == "(BC)265";
    }
    #endregion
    
    private void RegisterConfig(IGenericModConfigMenuApi configMenu) {
        configMenu.Register(
            mod: ModManifest,
            reset: () => Config = new Configuration(),
            save: () => Helper.WriteConfig(Config)
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.Enabled,
            getValue: () => Config.Enabled,
            setValue: value => Config.Enabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.JarsEnabled, 
            getValue: () => Config.JarsEnabled,
            setValue: value => Config.JarsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.KegsEnabled, 
            getValue: () => Config.KegsEnabled,
            setValue: value => Config.KegsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.CasksEnabled, 
            getValue: () => Config.CasksEnabled,
            setValue: value => Config.CasksEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.MayonnaiseMachinesEnabled, 
            getValue: () => Config.MayonnaiseMachinesEnabled,
            setValue: value => Config.MayonnaiseMachinesEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.CheesePressesEnabled, 
            getValue: () => Config.CheesePressesEnabled,
            setValue: value => Config.CheesePressesEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.LoomsEnabled, 
            getValue: () => Config.LoomsEnabled,
            setValue: value => Config.LoomsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.OilMakersEnabled, 
            getValue: () => Config.OilMakersEnabled,
            setValue: value => Config.OilMakersEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.DehydratorsEnabled, 
            getValue: () => Config.DehydratorsEnabled,
            setValue: value => Config.DehydratorsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.FishSmokersEnabled, 
            getValue: () => Config.FishSmokersEnabled,
            setValue: value => Config.FishSmokersEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.BaitMakersEnabled, 
            getValue: () => Config.BaitMakersEnabled,
            setValue: value => Config.BaitMakersEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.BoneMillsEnabled, 
            getValue: () => Config.BoneMillsEnabled,
            setValue: value => Config.BoneMillsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.CharcoalKilnsEnabled, 
            getValue: () => Config.CharcoalKilnsEnabled,
            setValue: value => Config.CharcoalKilnsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.CrystalariumsEnabled, 
            getValue: () => Config.CrystalariumsEnabled,
            setValue: value => Config.CrystalariumsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.FurnacesEnabled, 
            getValue: () => Config.FurnacesEnabled,
            setValue: value => Config.FurnacesEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.RecyclingMachinesEnabled, 
            getValue: () => Config.RecyclingMachinesEnabled,
            setValue: value => Config.RecyclingMachinesEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.SeedMakersEnabled, 
            getValue: () => Config.SeedMakersEnabled,
            setValue: value => Config.SeedMakersEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.SlimeEggPressesEnabled, 
            getValue: () => Config.SlimeEggPressesEnabled,
            setValue: value => Config.SlimeEggPressesEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.CrabPotsEnabled, 
            getValue: () => Config.CrabPotsEnabled,
            setValue: value => Config.CrabPotsEnabled = value
        );
        
        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.DeconstructorsEnabled, 
            getValue: () => Config.DeconstructorsEnabled,
            setValue: value => Config.DeconstructorsEnabled = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: I18n.BubbleYOffset,
            getValue: () => Config.OffsetY,
            setValue: value => Config.OffsetY = value,
            min: 0,
            max: 128
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: I18n.EmoteInterval,
            getValue: () => Config.EmoteInterval,
            setValue: value => Config.EmoteInterval = value,
            min: 0,
            max: 1000
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: I18n.Opacity,
            getValue: () => Config.OpacityPercent,
            setValue: value => Config.OpacityPercent = value,
            min: 1,
            max: 100
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: I18n.BubbleSize,
            getValue: () => Config.SizePercent,
            setValue: value => Config.SizePercent = value,
            min: 1,
            max: 100
        );
    }
}