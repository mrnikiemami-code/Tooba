# Module Layout Target

Current: flat `src/backend/Modules/<Name>/Tooba.<Name>.*`

Target physical (after ownership/dependency repair — **no move in this task**):

```
src/Modules/<Name>/{Domain,Application,Contracts,Infrastructure}
src/Host/Tooba.Host
```

Rule: dependency/ownership repair **before** cosmetic folder moves.
