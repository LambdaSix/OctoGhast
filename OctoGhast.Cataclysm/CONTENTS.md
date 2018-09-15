# What content goes in the Cata module?

Anything not required for core runtime behaviour. So in theory everything. However, for the sake of supporting other modules:

## JSON

* Belongs in Core
nb. It /might/ be worthwhile pulling very generic things out from CataModule for use by
other modules that either want to be a total conversion. Things like basic Materials and comestibles?

* Belongs in CataModule
** Item Groups 
** Ammo
** Armours
** Book
** Classes
** Comestibles
** Generic
** Gun
** GunMod
** Magazine
** Resources
** Tools
** Vehicles/Parts

## Loader/Item Code

* Belong in Core
** Type Loader
** Item (and the Component subparts)
** ToolQuality
** Material

* Belongs in CataModule
** LegacyLoader (Maps the existing cata json format to the internal Item format)

## Engine Parts
* Belong in Core
** Framework code

* Belongs in CataModule
** Implementation for specific features hooking into framework

## TODO

### Allow additional modules to override/add to existing item_groups.
Probably just implement the ItemGroup loader in such a way that all groups with matching ID's are concatenated together.

### Allow additional modules to override and add filters to the TypeLoader handlers
Adding support for new TYPE's would be useful for modules that want to load custom json for use in code.
Allowing overrides for existing types is less important, but could useful in a pass-through situation, the mod gets a look
at the raw json/object model and can tinker before letting CataModule load it.

For example: Hook into loading all monster types and divide their speed in half before passing it into the CataModule::Loader

### Engine framework
Provide a semi-generic way for CataModule to hook into some kind of basic classes. Enough that a new module
could depend on CataModule yet override specific things. Or add new functionality.

For example: Hook into the PreTurn() handler and handle custom medicines effects.

Main areas that need an API of some kind for modules:
	- Game Loop
	- Item Use
	- Item Examine
	- Furniture Examine/Use
	- Trap handling (placement/ticks/retrieval/activation)
	- Player effects
	- Player turn processing (same pre/during/post hooks?)