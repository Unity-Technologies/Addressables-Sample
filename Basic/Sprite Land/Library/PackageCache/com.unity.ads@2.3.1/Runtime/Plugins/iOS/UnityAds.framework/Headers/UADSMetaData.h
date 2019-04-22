#import "UADSJsonStorage.h"

@interface UADSMetaData : UADSJsonStorage

@property (nonatomic, strong) NSString *category;

- (instancetype)initWithCategory:(NSString *)category;
- (BOOL)setRaw:(NSString *)key value:(id)value;
- (void)commit;

@end
