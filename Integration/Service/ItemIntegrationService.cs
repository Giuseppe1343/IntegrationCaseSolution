using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is used to prevent multiple threads from saving the same item content at the same time.
    private readonly CriticalOperationProcessor<string> _processor = new();


    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        // Check the backend to see if the content is already saved.
        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            return new Result(false, $"Duplicate item received with content {itemContent}.");

        // SafeOperation ensures that only one thread at a time can be in the critical section for the same itemContent.
        return _processor.SafeOperation(itemContent, () =>
        {
            // Check the backend again, in case another thread saved the item while we were waiting. - a.k.a. "double check".
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
                return new Result(false, $"Duplicate item received with content {itemContent}.");

            var item = ItemIntegrationBackend.SaveItem(itemContent);
            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        });
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}