# Created / 201 response

Canonical ApiResponseFactory.Created(location, Result<T>) exists (Offer pattern).
AdminCreateAsync (articles) and CreateAsync (authors) now use api.Created(...).
Errors continue through api.From(result).
