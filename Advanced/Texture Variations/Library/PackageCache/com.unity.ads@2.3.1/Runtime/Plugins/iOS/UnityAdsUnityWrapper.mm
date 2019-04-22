#import "UnityAppController.h"
#import "Unity/UnityInterface.h"

#import "UnityAds/UnityAds.h"
#import "UnityAds/UADSPurchasing.h"
#import "UnityAds/UADSMetaData.h"

extern "C" {

    const char * UnityAdsCopyString(const char * string) {
        char * copy = (char *)malloc(strlen(string) + 1);
        strcpy(copy, string);
        return copy;
    }

    typedef void (*UnityAdsReadyCallback)(const char * placementId);
    typedef void (*UnityAdsDidErrorCallback)(long rawError, const char * message);
    typedef void (*UnityAdsDidStartCallback)(const char * placementId);
    typedef void (*UnityAdsDidFinishCallback)(const char * placementId, long rawFinishState);

    typedef void (*UnityAdsPurchasingDidInitiatePurchasingCommandCallback)(const char * eventString);
    typedef void (*UnityAdsPurchasingGetProductCatalogCallback)();
    typedef void (*UnityAdsPurchasingGetPurchasingVersionCallback)();
    typedef void (*UnityAdsPurchasingInitializeCallback)();

    static UnityAdsReadyCallback readyCallback = NULL;
    static UnityAdsDidErrorCallback errorCallback = NULL;
    static UnityAdsDidStartCallback startCallback = NULL;
    static UnityAdsDidFinishCallback finishCallback = NULL;

    static UnityAdsPurchasingDidInitiatePurchasingCommandCallback iapCommandCallback = NULL;
    static UnityAdsPurchasingGetProductCatalogCallback iapCatalogCallback = NULL;
    static UnityAdsPurchasingGetPurchasingVersionCallback iapVersionCallback = NULL;
    static UnityAdsPurchasingInitializeCallback iapInitializeCallback = NULL;

}

@interface UnityAdsUnityWrapperDelegate : NSObject <UnityAdsDelegate, UADSPurchasingDelegate>
@end

@implementation UnityAdsUnityWrapperDelegate

- (void)unityAdsReady:(NSString *)placementId {
    if(readyCallback != NULL) {
        const char * rawPlacementId = UnityAdsCopyString([placementId UTF8String]);
        readyCallback(rawPlacementId);
        free((void *)rawPlacementId);
    }
}

- (void)unityAdsDidError:(UnityAdsError)error withMessage:(NSString *)message {
    if(errorCallback != NULL) {
        const char * rawMessage = UnityAdsCopyString([message UTF8String]);
        errorCallback(error, rawMessage);
        free((void *)rawMessage);
    }
}

- (void)unityAdsDidStart:(NSString *)placementId {
    UnityPause(1);
    if(startCallback != NULL) {
        const char * rawPlacementId = UnityAdsCopyString([placementId UTF8String]);
        startCallback(rawPlacementId);
        free((void *)rawPlacementId);
    }
}

- (void)unityAdsDidFinish:(NSString *)placementId withFinishState:(UnityAdsFinishState)state {
    UnityPause(0);
    if(finishCallback != NULL) {
        const char * rawPlacementId = UnityAdsCopyString([placementId UTF8String]);
        finishCallback(rawPlacementId, state);
        free((void *)rawPlacementId);
    }
}

- (void)unityAdsPurchasingDidInitiatePurchasingCommand:(NSString *)eventString {
    if(iapCommandCallback != NULL) {
        const char * rawEventString = UnityAdsCopyString([eventString UTF8String]);
        iapCommandCallback(rawEventString);
        free((void *)rawEventString);
    }
}

- (void)unityAdsPurchasingGetProductCatalog {
    if(iapCatalogCallback != NULL) {
        iapCatalogCallback();
    }
}

- (void)unityAdsPurchasingGetPurchasingVersion {
    if(iapVersionCallback != NULL) {
        iapVersionCallback();
    }
}

- (void)unityAdsPurchasingInitialize {
    if(iapInitializeCallback != NULL) {
        iapInitializeCallback();
    }
}

@end

extern "C" {

    void UnityAdsInitialize(const char * gameId, bool testMode) {
        static UnityAdsUnityWrapperDelegate * unityAdsUnityWrapperDelegate = NULL;
        if(unityAdsUnityWrapperDelegate == NULL) {
            unityAdsUnityWrapperDelegate = [[UnityAdsUnityWrapperDelegate alloc] init];
        }
        [UnityAds initialize:[NSString stringWithUTF8String:gameId] delegate:unityAdsUnityWrapperDelegate testMode:testMode];
        [UADSPurchasing initialize:unityAdsUnityWrapperDelegate];
    }

    void UnityAdsPurchasingDispatchReturnEvent(UnityAdsPurchasingEvent event, const char * payload) {
        if (payload == NULL) {
            payload = "";
        }
        [UADSPurchasing dispatchReturnEvent:event withPayload:[NSString stringWithUTF8String:payload]];
    }

    void UnityAdsShow(const char * placementId) {
        if(placementId == NULL) {
            [UnityAds show:UnityGetGLViewController()];
        } else {
            [UnityAds show:UnityGetGLViewController() placementId:[NSString stringWithUTF8String:placementId]];
        }
    }

    bool UnityAdsGetDebugMode() {
        return [UnityAds getDebugMode];
    }

    void UnityAdsSetDebugMode(bool debugMode) {
        [UnityAds setDebugMode:debugMode];
    }

    bool UnityAdsIsSupported() {
        return [UnityAds isSupported];
    }

    bool UnityAdsIsReady(const char * placementId) {
        if(placementId == NULL) {
            return [UnityAds isReady];
        } else {
            return [UnityAds isReady:[NSString stringWithUTF8String:placementId]];
        }
    }

    long UnityAdsGetPlacementState(const char * placementId) {
        if(placementId == NULL) {
            return [UnityAds getPlacementState];
        } else {
            return [UnityAds getPlacementState:[NSString stringWithUTF8String:placementId]];
        }
    }

    const char * UnityAdsGetVersion() {
        return UnityAdsCopyString([[UnityAds getVersion] UTF8String]);
    }

    bool UnityAdsIsInitialized() {
        return [UnityAds isInitialized];
    }

    void UnityAdsSetMetaData(const char * category, const char * data) {
        if(category != NULL && data != NULL) {
            UADSMetaData* metaData = [[UADSMetaData alloc] initWithCategory:[NSString stringWithUTF8String:category]];
            NSDictionary* json = [NSJSONSerialization JSONObjectWithData:[[NSString stringWithUTF8String:data] dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
            for(id key in json) {
                [metaData set:key value:[json objectForKey:key]];
            }
            [metaData commit];
        }
    }

    void UnityAdsSetReadyCallback(UnityAdsReadyCallback callback) {
        readyCallback = callback;
    }

    void UnityAdsSetDidErrorCallback(UnityAdsDidErrorCallback callback) {
        errorCallback = callback;
    }

    void UnityAdsSetDidStartCallback(UnityAdsDidStartCallback callback) {
        startCallback = callback;
    }

    void UnityAdsSetDidFinishCallback(UnityAdsDidFinishCallback callback) {
        finishCallback = callback;
    }

    void UnityAdsSetDidInitiatePurchasingCommandCallback(UnityAdsPurchasingDidInitiatePurchasingCommandCallback callback) {
        iapCommandCallback = callback;
    }

    void UnityAdsSetGetProductCatalogCallback(UnityAdsPurchasingGetProductCatalogCallback callback) {
        iapCatalogCallback = callback;
    }

    void UnityAdsSetGetVersionCallback(UnityAdsPurchasingGetPurchasingVersionCallback callback) {
        iapVersionCallback = callback;
    }

    void UnityAdsSetInitializePurchasingCallback(UnityAdsPurchasingInitializeCallback callback) {
        iapInitializeCallback = callback;
    }
}

