# Extended Player Inventory: saved row repair

The vanilla character key `invrows` is the base row count. The effective inventory height is that base plus `ExtraRows` and, when enabled, one equipment/quick-slot row. Saving the expanded height as the base caused repeated growth on respawn. This fix saves only the base.

When equipment is displayed in its separate panel, the ordinary inventory background excludes that row's height. The underlying inventory still retains the equipment/quick-slot row. Inline equipment and disabled-equipment layouts use their full visible height.

## Automatic repair

`Maintenance.AutoRepairSavedRows` defaults to `true`. On a local character's spawn, the mod derives the vanilla base from character purchase flags:

| Purchase flags | Base rows |
| --- | --- |
| Neither | 4 |
| `invslot1` or `invslot2` | 5 |
| Both | 6 |

If the saved base exceeds this value, the repair plans a complete layout before changing any live item. Actual worn equipment and quick slots are preserved, valid ordinary positions stay in place, and overflow/collisions are packed into free ordinary slots. `ExtraRows` is retained. Item stacks and custom data are unchanged.

If everything does not fit, sizing is cancelled without moving items, shrinking capacity, or marking the repair complete. Move items to storage and reload to retry. After confirmed sizing, the character receives the `ExtendedPlayerInventory_rows_checked_v1` key. The migration is not applied again to that character, and later purchases use normal sizing. Healthy saves are marked checked after their dimensions are confirmed. Undersized saves are left to normal sizing.

Detection is scoped to `Player.OnSpawned`: a trader can call `SetInventorySize` before recording the new purchase flag, so live purchase calls must not trigger migration.

## Other inventory mods and manual overrides

The purchase flags identify vanilla upgrades. They cannot identify extra base capacity supplied by another mod. If another mod writes additional capacity into `invrows`, disable automatic repair **before loading a character**:

```ini
[Maintenance]
AutoRepairSavedRows = false
```

Capacity configured through this mod's `ExtraRows` does not require opting out.

For a one-time explicit repair, set `RepairCharacter` to the exact character name and `RepairBaseRows` to the intended base (4–9), including purchased capacity but excluding `ExtraRows` and the equipment row. A matching manual request takes precedence over automatic detection and works with automatic mode disabled. It resets to zero only after confirmed success and also marks the character checked.

Back up character saves before migrating. The mod changes live character data; Valheim persists it through normal character saving. It does not edit `.fch` files directly.

## Validation

Run `Tests/Run.Tests.ps1` from PowerShell. The checks execute production sizing/repair code with isolated game stand-ins and cover repeated spawns, purchase combinations, live purchases, full-inventory abort/retry, item preservation, overrides, opt-out, character isolation, spawn-context cleanup, and separate/inline/disabled equipment panel heights. The plugin also compiles against Valheim 1.0.20/BepInEx assemblies. A local in-game repair restored a saved base of 57 to 5 (total height 9), with the repair log confirming 35 item stacks preserved. Visual confirmation of the panel-height correction, death/recovery checks, and multiplayer verification remain pending.
