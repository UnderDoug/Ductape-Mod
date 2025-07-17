# Ductape Mod

Adds utilitape, an item which, much like our own ductape, can be used to hold things together that might have otherwise not withstood the modifications being made to them.

Utilitape is an applicator item that can be crafted at a bit cost of "000" (3x A-D, chosen at random on world-gen) and requires a bandage as a crafting ingredient. When it's applied to an eligible item, it uses up -1 modification slot, allowing for an additional modification (excluding iteslf). It's possible to find spools of utilitape in a lot of the same locations you can find basic toolkits.

The drawback is that the item is now "held together by utilitape" and is at risk of being jostled apart. Several ways in which an item can be used will jostle the item and there's a chance each time it's jostled that it'll take damage equal to 1/4 of its hitpoints.

During a turn, multiple sources of jostling will be combined (only a single time each) and then tested against the chance to take damage. The more actively you use an item, the more likely it is per turn to receive damage.

Although the amount is only tiny, even simply walking around with a utilitaped item in your inventory, or that item drawing charge, is enough to jostle it and risk it being damaged.

Due to taking damage from activity and not being broken outright, the sturdy modification won't stop your item from being jostled into a broken state (once it receives damage greater than 75% of it's total). This should be ample time to repair it, especially since once it's broken it's much harder to use "actively" and is at lower risk (but not no risk) of taking further damage.

3 Modifiers is surprisingly well balanced, so the addition of a 4th needs to be at a significant cost to remain balanced. Options have been included to make jostling more or less likely to result in damaging the item so that the balance can be tweaked to the player's liking.

An item being held together by utilitape can be risky so it's (by default) only able to be applied to items that are already at their limit for modifications (typically 3 mods). The mod has a pair of options, one to lift this limitation and allow applying ductape to any otherwise eligible item, and another that reduces the likelihood that a jostled item takes damage, based on how many modifications it has applied (getting more likely as 3 is approached).