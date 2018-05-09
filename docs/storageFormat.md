# Storage Format

## Models

`{FQN context class}-{FQN model class}-{Id}`

Each file stores the json serialized output of the model. One "row".

Questions:

1. What about models with no Id field?
2. What if the Id is changed?
3. What if the Id is changed to a duplicate another model with the same Id?

## Metadata

`{FQN context class}-{FQN model class}-{metadata}`

Metadata file will store metadata about the StorageSet such as index inforamtion.