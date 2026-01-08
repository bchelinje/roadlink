import { NgModule, Optional, SkipSelf } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import {ApiModule} from './api';
import {API_CONFIG_PROVIDER} from './api-config.provider';

// Import the generated API module

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,
    ApiModule,
  ],
  providers: [
    API_CONFIG_PROVIDER,
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parentModule: CoreModule) {
    if (parentModule) {
      throw new Error('CoreModule is already loaded. Import it in AppModule only');
    }
  }
}
