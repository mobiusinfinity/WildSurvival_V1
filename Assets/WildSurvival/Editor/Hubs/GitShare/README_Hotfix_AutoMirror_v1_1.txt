AutoMirror V1.1 Hotfix — SearchOption.AllDirectories
====================================================
Fixes compile error:
 CS0117: 'SearchOption' does not contain a definition for 'AllDirectory'

What to do:
1) DELETE any duplicate AutoMirror files under:
   Assets/WildSurvival/Imports/**/Editor/Collab/CollabAutoMirrorV11.cs
   (or rename .cs -> .cs.txt)
2) Place THIS file at:
   Assets/WildSurvival/Editor/Collab/CollabAutoMirrorV11.cs
   (overwrite the old one)
3) Let Unity recompile.

Tip: Keep all authoritative Editor tools under Assets/WildSurvival/Editor/**,
and keep Imports/** code non-compiling (.cs.txt) unless explicitly promoted.