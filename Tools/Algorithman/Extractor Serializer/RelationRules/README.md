# Item relation rules

Item relations tell the server which item records are the same item at different
quality levels (the QL 1 record, the QL 200 record, and any in between), so it can
pick the right low and high record for a given QL. The Extractor Serializer works
them out when it builds `items.ocp`, in this order of trust:

1. `static_list.txt` below.
2. The families in `itemrelations.txt` (the relations aoitems.com supplied), if a
   copy sits in the folder the Extractor runs from. It is not in the repository.
3. `ItemRelationMatcher`: records with the same name and item type, in quality order,
   plus `nameseparation_list.txt` and `delete_list.txt` below.
4. Otherwise the item on its own.

The three lists started as copies of Tyrbot's `items_extractor/config` files and are
meant to be edited here. Lines starting with `#` are comments.

| File | Line format | Use it for |
|---|---|---|
| `static_list.txt` | `lowid,highid,lowql,highql[,icon,name]` | Items whose records are scattered through the database, which name matching cannot find. Chain the pairs: one line's high id is the next line's low id. |
| `nameseparation_list.txt` | `pattern1,pattern2,itemtype` | One item sold under two names at different QLs, such as `Senpai %,Hanshi %,Armor`. `%` matches any text; item type is Misc, Weapon, Armor, Implant, Template or Spirit. |
| `delete_list.txt` | an SQL-style condition: `icon = N`, `aoid = N`, `name = '...'`, `UPPER(name) LIKE UPPER('...')`, joined with `AND` | Records never to pair by name. **Not used for extraction** (Tyrbot uses it to keep NPC attacks, implants and test records out of its item search; the server needs their relations). Only `--check-relations --delete-list` reads it. |

After editing, rebuild the Extractor Serializer (the lists are copied next to the
exe) and see the effect before extracting:

```
"Extractor Serializer.exe" --check-relations <path to itemrelations.txt> [--delete-list]
```

It compares the matcher's families with aoitems.com's and writes nothing. Then run the
extractor normally to rebuild `items.ocp`.
