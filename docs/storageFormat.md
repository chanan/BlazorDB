# Storage Format

## Models

`{FQN context class}-{FQN model class}-{Guid}`

Contents:

* Each file stores the json serialized output of the model. One "row".

## Metadata

`{FQN context class}-{FQN model class}-{metadata}`

Contents:

* Context FQN
* StorageSet FQN
* Guids - List of persisted guids

Initial implementation will regenerate the guid on every `SaveChanges()` and the list in the metadata table. Future implementation might store metadata about the model in the model value itself, so the guid will be loaded into memory and won't be regenerated.  

Future items that might be stored in the the metadata file would be items such as index information