import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ChatRoutingModule } from './chat-routing.module';
import { ChatContainerComponent } from './components/chat-container/chat-container.component';
import { SharedMaterialModule } from '../../shared/shared-material.module';

@NgModule({
  declarations: [ChatContainerComponent],
  imports: [CommonModule, ChatRoutingModule, SharedMaterialModule],
})
export class ChatModule {}
