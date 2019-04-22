# Custom Operations

The `IResourceProvider` API allows extending the loading process by defining locations and dependencies in a data driven manner. In some cases though, you might want to create a custom operation. The `IResourceProvider` API is internally built on top of these custom operations.

You can create custom operations by deriving from the `AsyncOperationBase` class and overriding the desired virtual methods. You can pass the derived operation to the `ResourceManager.StartOperation` method to start the operation and get an `AsyncOperationHandle`. Operations started this way are registered with the `ResourceManager` and will show up in the Addressables profiler.

The `AsyncOperationBase.Execute` method will be invoked on your operation after the optional dependent operation completes.

When your operation completes, you should call `AsyncOperationBase.Complete` on your custom operation object. You can call this within the `Execute` call or defer it to outside the call. Calling `AsyncOperationBase.Complete` notifies the `ResourceManager` that the operation has completed and will invoke the associated AsyncOperationHandle.Completed events.

When the user Releases the `AsyncOperationHandle` that references the custom operation, the `ResourceManager` will call `AsyncOperationBase.Destroy` on your operation. This is where you should release and memory or resources associated with your operation.