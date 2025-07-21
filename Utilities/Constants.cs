using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class Constants
    {
        public enum TM_MappingCollectionType
        {
            NONE = 0,
            PRODUCT = 1,
            PRODUCT_UNIT = 2,
            PRODUCT_INVENTORY = 3,
            PRODUCT_VARIANT = 4,
            PRODUCT_VARIANT_INVENTORY = 5,
            PRODUCT_CATEGORY = 6,
            LOCATION = 7,
            CUSTOMER = 8,
            TRANSACTION = 9,
            TRANSACTION_LINE = 10,
            TRANSACTION_ADDRESS = 11,
            TRANSACTION_TAX = 12,
            TRANSACTION_PAYMENT = 13,
            TRANSACTION_TRACKING_NUMBER = 14,
            TRANSACTION_NOTE = 15,
            CUSTOMER_CATEGORY = 16,
            CUSTOMER_ADDRESS = 17,
            CUSTOMER_CONTACT = 18,
            SHIPMENT = 19,
            SHIPMENT_LINE = 20,
            SHIPPING_METHOD = 21,
            PAYMENT_METHOD = 22,
            PRODUCT_OPTION = 23,
            PRODUCT_OPTION_VALUE = 24,
            PRODUCT_VARIANT_OPTION = 25,
            PRODUCT_VARIANT_OPTION_VALUE = 26,
            GIFT_CARD = 27,
            GIFT_CARD_ACTIVITY = 28,
            TRANSACTION_DISCOUNT = 29,
            CATALOG_CATEGORY_SET = 30,
            KIT = 31,
            KIT_COMPONENT = 32,
            ALTERNATE_ID_TYPE = 33,
            PRODUCT_ALTERNATE_ID = 34,
            PRODUCT_VARIANT_ALTERNATE_ID = 35,
            PRODUCT_CATEGORY_ASSIGNMENT = 36,
            PRODUCT_VARIANT_CATEGORY_ASSIGNMENT = 37,
            LOCATION_GROUP = 38,
            VARIANT_KIT = 39,
            VARIANT_KIT_COMPONENT = 40,
            PRODUCT_RELATED_PRODUCT = 41,
            VARIANT_RELATED_PRODUCT = 42,
            EMPLOYEE = 43,
            EMPLOYEE_ADDRESS = 44,
            TIMESHEET = 45,
            TIMESHEET_ENTRY = 46,
            TRANSACTION_LINE_DISCOUNT = 47,
            MESSAGE = 48,
            CUSTOMER_COMPANY = 49,
            CUSTOMER_COMPANY_ADDRESS = 50,
            COMPANY_RELATIONSHIP = 51,
            CUSTOMER_RELATIONSHIP = 52,
            CUSTOMER_COMPANY_CATEGORY = 53
        }
    }
}
