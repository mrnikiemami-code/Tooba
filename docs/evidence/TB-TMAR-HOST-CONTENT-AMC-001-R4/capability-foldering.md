# Capability foldering — R4

```
Tooba.Content.Application/
  Articles/{Commands,Queries,Models,Ports,Validators}
  Authors/{Commands,Queries,Models,Ports,Validators}
  Categories/{Commands,Queries,Models,Ports,Validators}
  Tags/{Commands,Queries,Models,Ports,Validators}
  Media/{Commands,Queries,Models,Ports,Validators}
  Comments/{Commands,Queries,Models,Ports,Validators}
  Composition/ContentOperation.cs
  Validators/ContentValidationCodes.cs
```

- Capability-first, shallow-by-default
- ZERO unjustified one-file-per-Command/Query leaf folders
- Manifest forbids top-level Commands/Queries/Models/Ports
