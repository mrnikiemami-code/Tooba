# Mixed Real + Fake

`applyPreviewFill` keeps all real items first, appends only `target - realCount` fakes. Guard asserts 1 real review + fake fills. Never replaces all with fakes when real exist.
