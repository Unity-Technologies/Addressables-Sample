/**
 *  An enumeration for the callback type returned via event.
 */
typedef NS_ENUM(NSInteger, UnityAdsPurchasingEvent) {
    /**
     *  An event that indicates the success or failure of a command sent to Purchasing.
     */
    kUnityAdsPurchasingEventPurchasingCommandCallback,
    /**
     *  An event that indicates the version of the Purchasing asset package.
     */
    kUnityAdsPurchasingEventPurchasingVersion,
    /**
     *  An event that indicates the current product catalog from the Purchasing asset pacakge.
     */
    kUnityAdsPurchasingEventProductCatalog,
    /**
     *  An event that indicates the success or failure of initializing Purchasing.
     */
    kUnityAdsPurchasingEventInitializationResult,
    /**
     *  An event that indicates a message directly from Purchasing.
     */
    kUnityAdsPurchasingEventPurchasingEvent
};
/**
 *  The `UADSPurchasingDelegate` protocol defines the required methods for receiving IAP promo-related messages from UnityAds.
 *  Implemented by the asset package.
 *  @note These selectors return callback responses to the UnityAds webview via events.
 */
NS_ASSUME_NONNULL_BEGIN
@protocol UADSPurchasingDelegate <NSObject>
/**
 *  Called when `UnityAds` needs to fetch the version of the Purchasing asset package.
 *
 */
- (void)unityAdsPurchasingGetPurchasingVersion;
/**
 *  Called when `UnityAds` needs to fetch catalog of products currently available for purchase.
 *
 */
- (void)unityAdsPurchasingGetProductCatalog;
/**
 *  Called when an in-app purchase is initiated from an ad.
 *
 *  @param eventString The string provided via the ad.
 */
- (void)unityAdsPurchasingDidInitiatePurchasingCommand:(NSString *)eventString;
/**
 *  Called when `UnityAds` needs to initialize Purchasing.
 *
 */
- (void)unityAdsPurchasingInitialize;
@end

/**
 * `UADSPurchasing` is a static class with methods initializing with a delegate and dispatching return values to the webview.
 *
 * @warning In order to ensure expected behaviour, the delegate must always be set.
 */

@interface UADSPurchasing : NSObject

- (instancetype)init NS_UNAVAILABLE;
+ (instancetype)initialize NS_UNAVAILABLE;

/**
 *  Initializes UnityAds Purchasing. Should be initialized when app starts.
 *
 *  @param delegate delegate for UADSPurchasing callbacks
 */
+ (void)initialize:(nullable id<UADSPurchasingDelegate>)delegate;
/**
 *  Dispatches a callback to the UnityAds webview
 *
 *  @param event the type of event to be dispatched
 *  @param payload the string payload to be dispatched to the webview
 */
+ (void)dispatchReturnEvent:(UnityAdsPurchasingEvent)event withPayload:(NSString *)payload;

@end
NS_ASSUME_NONNULL_END
