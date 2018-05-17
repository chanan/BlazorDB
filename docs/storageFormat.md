# Storage Format

## Models

`{FQN context class}-{FQN model class}-{Guid}`

Contents:

* Each file stores the json serialized output of the model. One "row".

### Associations

**One Association**

For objects that contain other object not in the same context, they will be serialized as one object, for example (`Context.cs`):

```
Person with an Address:
{"Id":1,"FirstName":"John","LastName":"Smith","HomeAddress":{"Id":0,"Street":"221 Baker Street","City":"This should be part of the json, since address is not in context (Very long city)"}}
```

For objects that contain object in the same context, they will be serialzied with a "foreign key", for example (`AssociationContext.cs`):

```
Address:
{"Id":1,"Street":"Changed Streeet","City":"This should be a refrence to address since Address exists in the context"}

Person:
{"Id":1,"FirstName":"John","LastName":"Smith","HomeAddress":1}

```

**Many Association**

Many associations are stored as an array of ids:

```
{"Id":1,"FirstName":"Many","LastName":"Test","HomeAddress":null,"OtherAddresses":[1,2]}
```

## Metadata

`{FQN context class}-{FQN model class}-{metadata}`

Contents:

* Guids - List of persisted guids

Initial implementation will regenerate the guid on every `SaveChanges()` and the list in the metadata table. Future implementation might store metadata about the model in the model value itself, so the guid will be loaded into memory and won't be regenerated.  

Future items that might be stored in the the metadata file would be items such as index infromation