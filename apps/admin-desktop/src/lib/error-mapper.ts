import { ApiError } from './api-client';

export interface UserFriendlyError {
  title: string;
  message: string;
  action?: string;
}

/**
 * Maps backend errors to user-friendly messages
 */
export class ErrorMapper {
  private static readonly ERROR_MAPPINGS: Record<string, UserFriendlyError> = {
    // Stock errors
    'insufficient_stock': {
      title: 'Insufficient Stock',
      message: 'There is not enough stock available for this operation.',
      action: 'Please check stock levels and try again.'
    },
    'negative_stock_not_allowed': {
      title: 'Negative Stock Not Allowed',
      message: 'This operation would result in negative stock.',
      action: 'Please adjust the quantity.'
    },
    
    // Sales errors
    'over_ship': {
      title: 'Over-Shipment Not Allowed',
      message: 'Cannot ship more than ordered quantity.',
      action: 'Please check the sales order remaining quantities.'
    },
    'over_invoice': {
      title: 'Over-Invoice Not Allowed',
      message: 'Cannot invoice more than shipped quantity.',
      action: 'Please check the shipment remaining quantities.'
    },
    'already_invoiced_line': {
      title: 'Line Already Invoiced',
      message: 'This shipment line has already been fully invoiced.',
      action: 'Please refresh and check the current status.'
    },
    'order_not_confirmed': {
      title: 'Order Not Confirmed',
      message: 'This sales order must be confirmed before creating a shipment.',
      action: 'Please confirm the order first.'
    },
    'shipment_not_shipped': {
      title: 'Shipment Not Shipped',
      message: 'This shipment must be shipped before creating an invoice.',
      action: 'Please ship the shipment first.'
    },
    'invoice_not_issued': {
      title: 'Invoice Not Issued',
      message: 'This invoice must be issued before creating a payment.',
      action: 'Please issue the invoice first.'
    },
    
    // Purchase errors
    'over_receive': {
      title: 'Over-Receipt Not Allowed',
      message: 'Cannot receive more than ordered quantity.',
      action: 'Please check the purchase order remaining quantities.'
    },
    'po_not_confirmed': {
      title: 'Purchase Order Not Confirmed',
      message: 'This purchase order must be confirmed before creating a goods receipt.',
      action: 'Please confirm the purchase order first.'
    },
    
    // Currency errors
    'currency_mismatch': {
      title: 'Currency Mismatch',
      message: 'The currency does not match.',
      action: 'Please ensure all related documents use the same currency.'
    },
    
    // Party errors
    'party_not_customer': {
      title: 'Not a Customer',
      message: 'This party is not marked as a customer.',
      action: 'Please use a party with type CUSTOMER for sales.'
    },
    'party_not_supplier': {
      title: 'Not a Supplier',
      message: 'This party is not marked as a supplier.',
      action: 'Please use a party with type SUPPLIER for purchases.'
    },
    
    // Duplicate errors
    'duplicate_code': {
      title: 'Duplicate Code',
      message: 'A record with this code already exists.',
      action: 'Please use a unique code.'
    },
    'duplicate_sku': {
      title: 'Duplicate SKU',
      message: 'A variant with this SKU already exists.',
      action: 'Please use a unique SKU.'
    },
    
    // State errors
    'invalid_state_transition': {
      title: 'Invalid State',
      message: 'This action is not allowed in the current state.',
      action: 'Please check the current status and try again.'
    },
    'cannot_cancel_invoiced': {
      title: 'Cannot Cancel',
      message: 'Cannot cancel a document that has been invoiced.',
      action: 'Please cancel the invoice first.'
    },
    
    // Payment errors
    'insufficient_funds': {
      title: 'Insufficient Funds',
      message: 'The cashbox or bank account does not have sufficient funds.',
      action: 'Please check the balance.'
    },
    'payment_amount_mismatch': {
      title: 'Amount Mismatch',
      message: 'Payment amount does not match the expected amount.',
      action: 'Please verify the amount.'
    },
  };

  static mapError(error: unknown): UserFriendlyError {
    // Handle ApiError
    if (error instanceof ApiError) {
      const { status, data } = error;

      // 401 - Unauthorized
      if (status === 401) {
        return {
          title: 'Session Expired',
          message: 'Your session has expired. Please log in again.',
          action: 'Redirecting to login...'
        };
      }

      // 403 - Forbidden
      if (status === 403) {
        return {
          title: 'Permission Denied',
          message: 'You do not have permission to perform this action.',
          action: 'Please contact your administrator.'
        };
      }

      // 409 - Conflict (domain errors)
      if (status === 409) {
        // Try to extract error code from response
        const errorCode = data?.code || data?.error || data?.message;
        
        if (typeof errorCode === 'string') {
          const mappedError = this.ERROR_MAPPINGS[errorCode];
          if (mappedError) {
            return mappedError;
          }
          
          // Try to extract from message
          for (const [code, mapping] of Object.entries(this.ERROR_MAPPINGS)) {
            if (errorCode.toLowerCase().includes(code.toLowerCase().replace(/_/g, ' '))) {
              return mapping;
            }
          }
        }

        return {
          title: 'Operation Conflict',
          message: typeof data === 'string' ? data : (data?.message || 'The operation cannot be completed due to a conflict.'),
          action: 'Please check the current state and try again.'
        };
      }

      // 400 - Bad Request (validation errors)
      if (status === 400) {
        const message = typeof data === 'string' ? data : (data?.message || 'Invalid input provided.');
        return {
          title: 'Validation Error',
          message,
          action: 'Please check your input and try again.'
        };
      }

      // 404 - Not Found
      if (status === 404) {
        return {
          title: 'Not Found',
          message: 'The requested resource was not found.',
          action: 'Please check the URL and try again.'
        };
      }

      // 500 - Server Error
      if (status >= 500) {
        return {
          title: 'Server Error',
          message: 'An unexpected error occurred on the server.',
          action: 'Please try again later or contact support.'
        };
      }

      // Other HTTP errors
      return {
        title: `Error ${status}`,
        message: typeof data === 'string' ? data : (data?.message || error.statusText),
        action: 'Please try again.'
      };
    }

    // Handle regular Error
    if (error instanceof Error) {
      return {
        title: 'Error',
        message: error.message,
        action: 'Please try again.'
      };
    }

    // Unknown error
    return {
      title: 'Unknown Error',
      message: 'An unexpected error occurred.',
      action: 'Please try again or contact support.'
    };
  }

  /**
   * Check if error requires redirect to login
   */
  static requiresLogin(error: unknown): boolean {
    return error instanceof ApiError && error.status === 401;
  }

  /**
   * Extract field validation errors from 400 response
   */
  static extractFieldErrors(error: unknown): Record<string, string> | null {
    if (!(error instanceof ApiError) || error.status !== 400) {
      return null;
    }

    const { data } = error;
    if (data?.errors && typeof data.errors === 'object') {
      return data.errors;
    }

    return null;
  }
}
