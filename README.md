# BlazorDB
In memory, persisted to localstorage, database for .net Blazor browser framework

## Warning
This library like Blazor itself is experimental and API is likely to change.

## Docs

### Install
So far the the API is very simplestic but works.

First add a reference to the nuget package:

```
Install-Package BlazorDB -Version 0.0.4
```

or

```
dotnet add package BlazorDB --version 0.0.4
```

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

*NOTE:* Models stored by BlazorDB require that an int Id property exist on the model. The Id property will be maintained by BlazorDB, you dont need to set it yourself.

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

So far, only one to one associations work.

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

Therefore, like in `Associations.cshtml` example, chaning the Address will Change the Person's HomeAddress:

```
Context.People[0].HomeAddress.Street = "Changed Streeet";
Context.SaveChanges();
Console.WriteLine("Person address changed: {0}", Context.People[0].HomeAddress.Street);
Console.WriteLine("Address entity changed as well: {0}", Context.Addresses[0].Street);
StateHasChanged();
```


## Example

A Todo sample built with BlazorDB is included in the sample project:

* [Todos.cshtml](https://github.com/chanan/BlazorDB/blob/master/src/Sample/Pages/Todos.cshtml)
* [TodoItemForm.cshtml](https://github.com/chanan/BlazorDB/blob/master/src/Sample/Pages/TodoItemForm.cshtml)

## Storage Format

[Storage Format Doc](https://github.com/chanan/BlazorDB/blob/master/docs/storageFormat.md)