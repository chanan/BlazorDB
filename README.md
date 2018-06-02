# BlazorDB
In memory, persisted to localstorage, database for .net Blazor browser framework

## Warning
This library like Blazor itself is experimental and API is likely to change.

## Docs

### Install

First add a reference to the nuget package:

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/BlazorDB.svg)](https://www.nuget.org/packages/BlazorDB/)

Then in Program.cs add Blazor DB to the dependency injection services:

```
var serviceProvider = new BrowserServiceProvider(services =>
{
    services.AddBlazorDB(options =>
    {
        options.LogDebug = true;
        options.Assembly = typeof(Program).Assembly;
    });
});
new BrowserRenderer(serviceProvider).AddComponent<App>("app");
```

Set `LogDebug` to see debug output in the browser console.

### Setup

**NOTE:** Models stored by BlazorDB require that an int Id property exist on the model. The Id property will be maintained by BlazorDB, you dont need to set it yourself.

Create at least one model and context for example:

Person.cs:

```
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Address HomeAddress { get; set; }
}
```

if the field `public int Id { get; set; }` exists it will be managed by BlazorDB

and a context, for example, Context.cs:
```
public class Context : StorageContext
{
    public StorageSet<Person> People { get; set; }
}
```

### Usage

See the full example in the sample app: https://github.com/chanan/BlazorDB/blob/master/src/Sample/Pages/Index.cshtml

Inject your context into your component:

```
@using Sample.Models
@inject Context Context
```

Create a model and add it to your Context:

```
var person = new Person { FirstName = "John", LastName = "Smith" };
Context.People.Add(person);
```

Do not set the Id field. It will be assigned by BlazorDB.

Call SaveChanges:

```
Context.SaveChanges();
```

Once `SaveChanges()` has been called, you may close the browser and return later, the data will be reloaded from localStorage.

You may query the data using linq for example:

```
private Person Person { get; set; }
void onclickGetPerson()
{
    var query = from person in Context.People
                where person.Id == 1
                select person;
    Person = query.Single();
    StateHasChanged();
}
```

## Associations

Associations work in the same context. If you have an object in another object that is not in the context, it will be serialized to localStorage as one "complex" document.

For example, in `Context.cs` only Person is in the Context and Address is not. Therefore, Person will contain Address, and Address will not be a seperate object.

### One to One Association

When an object refers to another object that are both in Context, they are stored as a reference, such that changing the reference will update both objects.

For example, `AssociationContext.cs`:


```
public class AssociationContext : StorageContext
{
    public StorageSet<Person> People { get; set; }
    public StorageSet<Address> Addresses { get; set; }
}
```

`Person.cs` as shown above has a property `public Address HomeAddress { get; set; }`. Because unlike `Context.cs`, `AssociationContext.cs` does define `public StorageSet<Address> Addresses { get; set; }` references are stored as "foreign keys" instead of complex objects.

Therefore, like in `Associations.cshtml` example, changing the Address will Change the Person's HomeAddress:

```
Context.People[0].HomeAddress.Street = "Changed Streeet";
Context.SaveChanges();
Console.WriteLine("Person address changed: {0}", Context.People[0].HomeAddress.Street);
Console.WriteLine("Address entity changed as well: {0}", Context.Addresses[0].Street);
StateHasChanged();
```
### One to Many, Many to Many Association

Define a "One" association by adding a property of the other model. For example in `Person.cs`:

```
public Address HomeAddress { get; set; }
```

Define a "Many" association by adding a property of type `List<>` to the association. For example in `Person.cs`:

```
public List<Address> OtherAddresses { get; set; }
```

This is association is then used in `Associations.cshtml` like so:

```
var person = new Person { FirstName = "Many", LastName = "Test" };
person.HomeAddress = new Address { Street = "221 Baker Streeet", City = "This should be a refrence to address since Address exists in the context" };
var address1 = new Address { Street = "Many test 1", City = "Saved as a reference" };
var address2 = new Address { Street = "Many test 2", City = "Saved as a reference" };
person.OtherAddresses = new List<Address> { address1, address2 };
Context.People.Add(person);
Context.SaveChanges();
StateHasChanged();
```

### Maintaining Associations

As you can see in the example above BlazorDB will detect associations added to the model so no need to add them to the Context explicitly. In the example above, the address objects do not need to be explicitly added to the context, instead they are persisted when the person object is added and `SaveChanges()` is called.

**Note:** At this time removing/deleting is not done automatically and needs to be done manually. A future update of BlazorDB will handle deletions properly.  

## Example

A Todo sample built with BlazorDB is included in the sample project:

* [Todos.cshtml](https://github.com/chanan/BlazorDB/blob/master/src/Sample/Pages/Todos.cshtml)
* [TodoItemForm.cshtml](https://github.com/chanan/BlazorDB/blob/master/src/Sample/Pages/TodoItemForm.cshtml)

## Fluxor Integration Example

The [Fluxor integration example](https://github.com/chanan/BlazorDB/tree/master/src/FluxorIntegration) shows how to use BlazorDB to manage data and Fluxor to connect between the UI and the data layer.

## Storage Format

[Storage Format Doc](https://github.com/chanan/BlazorDB/blob/master/docs/storageFormat.md)