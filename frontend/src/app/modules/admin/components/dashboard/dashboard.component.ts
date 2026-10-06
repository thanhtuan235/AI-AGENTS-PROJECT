import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent {
  // Mock Data Danh sách Tài liệu RAG
  documentColumns: string[] = [
    'title',
    'code',
    'effectiveDate',
    'status',
    'actions',
  ];
  documentsList = [
    {
      title: 'Quy định công tác phí 2024',
      code: 'QD-2024-NH/01',
      effectiveDate: '01/01/2024',
      status: 'Indexed',
    },
    {
      title: 'Hướng dẫn cấp phát thiết bị CNTT',
      code: 'HD-2023-IT/05',
      effectiveDate: '15/05/2023',
      status: 'Indexed',
    },
    {
      title: 'Quy trình duyệt nghỉ phép Online',
      code: 'QT-2024-NS/02',
      effectiveDate: '10/02/2024',
      status: 'Processing',
    },
  ];

  // Mock Data Danh sách Yêu cầu Hành chính eOffice
  requestColumns: string[] = [
    'requestId',
    'creator',
    'type',
    'createdDate',
    'status',
    'actions',
  ];
  requestsList = [
    {
      requestId: 'REQ-1024',
      creator: 'Nguyễn Văn A',
      type: 'Đề nghị Thanh toán Công tác phí',
      createdDate: '24/09/2026',
      status: 'Pending Approval',
    },
    {
      requestId: 'REQ-1025',
      creator: 'Trần Thị B',
      type: 'Cấp thiết bị Laptop làm việc',
      createdDate: '25/09/2026',
      status: 'Approved',
    },
    {
      requestId: 'REQ-1026',
      creator: 'Lê Văn C',
      type: 'Đăng ký Phòng họp 301',
      createdDate: '26/09/2026',
      status: 'Processing',
    },
  ];
}
