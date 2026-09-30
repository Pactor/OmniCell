# Movement - how fast a character actually goes

A server that never checks movement lets a client walk through walls at any speed it likes,
and one that checks against the wrong number rejects honest players. This page is what has
been measured off retail rather than assumed. It is short because only one thing has been
measured so far.

## Run speed to metres per second

Capture `20260926-144005`, stream 8: the same character ran the same stretch of Borealis
twice, once with a run speed buff up and once without, and said in the recording's labels
what the client's own interface was showing.

Speed is read off the client's own `CharDCMoveMessage`, which carries the position it has
reached and `MillisecondsSincePreviousMove`. Distance over time, for every segment longer
than 0.3 metres:

| what the client showed | segments | metres per second |
|---|---|---|
| run speed 325, buffed | 34 | **6.02**, spread 6.02 to 6.14 |
| run speed 165, unbuffed | 40 | **5.44**, spread 5.43 to 5.53 |

Both are flat to a hundredth over dozens of samples, including single segments of thirty
metres, so the client is not approximating - it moves at an exact speed and a server can hold
it to one.

Two points give a line and nothing more, but the line is:

    metres per second  =  4.84 + runspeed / 276

which is worth writing down only as the thing a third measurement should be checked against.
A run at a very low speed - unbuffed, unequipped, on a fresh character - would say whether the
relation is really linear and whether 4.84 is the standing base, and it costs one more
recording.

The readings below the plateau, 4.2 to 4.8 m/s, are all segments where the character turned or
started from rest inside the segment. They are not a second gait.

### The wire does not carry the number the interface shows

`RunSpeed` is stat 156. The `FullCharacter` at the start of that stream reads **115**, not 325
and not 165, and it does not change when the buff comes off. What the client sends when the
buff is removed is:

```
client  CharacterActionMessage Action=RemoveBuff Parameter1=53019 Parameter2=162583
server  BuffMessage            Action=0 Instance=53019 NanoId=162583
```

and **no `StatMessage` for 156 follows**. So retail does not push an effective run speed at
all: it pushes the buff, and the client adds up the base stat and whatever the active nanos
and worn items contribute. A server of ours that wants to validate movement has to do that sum
itself, from the same nano and item data, rather than reading a stat off the character.

What is not yet known is the mapping from stat 156 plus modifiers to the 165 the interface
showed - 115 base and 165 displayed differ by 50, which is presumably worn items, but nothing
captured says so.
