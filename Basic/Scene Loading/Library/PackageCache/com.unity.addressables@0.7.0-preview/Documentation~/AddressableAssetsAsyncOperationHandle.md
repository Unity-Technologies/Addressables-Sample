# AsyncOperationHandle

The `AsyncOperationHandle` struct is returned from several methods in the Addressables API. The main purpose of the `AsyncOperationHandle` is to allow access to the status and result of an operation. The result of the operation will be vaild until you call `AsyncOperationHandle.Release` on the operation.

When the operation completes, the `AsyncOperationHandle.Status`  will be either `AsyncOperationStatus.Succeeded` or `AsyncOperationStatus.Failed`. If successful, you can access the result through the `AsyncOperationHandle.Result` property.

You can periodically check the status of the operation or register for a completed callback using the `AsyncOperationHandle.Complete` event.

When you no longer need the resource provided by an AsyncOperationHandle return through the Addressables API, you should release through the `Addressables.Release` method. See [Memory Management](AddressableAssetsCustomOperation) for more details

##### Typed vs Typeless
Most of the Addressables API will return a generic `AsyncOperationHandle<T>`. This allows type safety for the `AsyncOperationHandle.Completed` event and for the `AsyncOperationHandle.Result`. There is also a non-generic `AsyncOperationHandle`. You can convert between the generic and non-generic handles. You will get a runtime exception if you attempt to cast a non-generic handle to a generic handle of an incorrect type. Below is an example of this conversion.

```C#
AsyncOperationHandle<Texture2D> textureHandle = Addressables.LoadAsset<Texture2D>("mytexture");

// Convert the AsyncOperationHandle<Texture2D> to an AsyncOperationHandle
AsyncOperationHandle nonGenericHandle = textureHandle;

// Convert the AsyncOperationHandle to an AsyncOperationHandle<Texture2D>
AsyncOperationHandle<Texture2D> textureHandle2 = nonGenericHandle.Convert<Texture2D>();

// The exact type is required. This will throw and exception because Texture2D is required
AsyncOperationHandle<Texture> textureHandle3 = nonGenericHandle.Convert<Texture>();
```

##### AsyncOperationHandle Usage Examples

You can register for a completion event on `AsyncOperationHandle.Completed`:
```C#
private void TextureHandle_Completed(AsyncOperationHandle<Texture2D> handle)
{
	if (handle.Status == AsyncOperationStatus.Succeeded)
	{
		Texture2D result = handle.Result;
		// Texture ready for use
	}
}

void Start()
{
	AsyncOperationHandle<Texture2D> textureHandle = Addressables.LoadAsset<Texture2D>("mytexture");
	textureHandle.Completed += TextureHandle_Completed;
}
```

`AsyncOperationHandle` implements IEnumerator so it can be yielded in coroutines:

```C#
public IEnumerator Start()
{
	AsyncOperationHandle<Texture2D> handle = Addressables.Load<Texture2D>("mytexture");
	yield return handle;
	if (handle.Status == AsyncOperationStatus.Succeeded)
	{
		Texture2D texture = handle.Result;
		// Texture ready for use...
		
		// Done. Release resource
		Addressables.ReleaseHandle(handle);
	}
}
```

Async await is also supported through the `AsyncOperationHandle.Task` property.
```C#
public async Start()
{
	AsyncOperationHandle<Texture2D> handle = Addressables.Load<Texture2D>("mytexture");
	await handle.Task;
	// Task has completed. Be sure to check the Status has succeeded before getting the Result
}
```

