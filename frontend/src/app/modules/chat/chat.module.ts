import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ChatRoutingModule } from './chat-routing.module';
import { ChatContainerComponent } from './components/chat-container/chat-container.component';
import { SharedMaterialModule } from '../../shared/shared-material.module';
import { MarkdownModule } from 'ngx-markdown';
import { ChatApiService } from './services/chat-api.service';
import { SessionApiService } from './services/session-api.service';

@NgModule({
  declarations: [ChatContainerComponent],
  imports: [
    CommonModule,
    FormsModule,
    ChatRoutingModule,
    SharedMaterialModule,
    MarkdownModule.forRoot(),
  ],
  providers: [ChatApiService, SessionApiService],
})
export class ChatModule {}
