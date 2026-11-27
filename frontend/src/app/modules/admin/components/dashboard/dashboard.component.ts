import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent {
  chatOpen = true;
  chatMinimized = false;

  metrics = [
    { label: 'Hồ sơ đang xử lý', value: '03', note: '01 hồ sơ cần bổ sung', icon: 'inbox', iconClass: '' },
    { label: 'Phép năm còn lại', value: '08 ngày', note: 'Cập nhật từ Khối Nhân sự', icon: 'event_available', iconClass: 'orange' },
    { label: 'Lịch họp hôm nay', value: '02', note: 'Cuộc họp tiếp theo lúc 10:30', icon: 'calendar_today', iconClass: 'green' },
  ];

  recentRequests = [
    { id: 'TT-2026-00816', content: 'Thanh toán công tác tháng 10', status: 'Đang xử lý', statusClass: 'processing' },
    { id: 'TB-2026-00189', content: 'Cấp màn hình bổ sung', status: 'Cần bổ sung', statusClass: 'pending' },
    { id: 'PH-2026-00423', content: 'Đặt phòng họp 05/11', status: 'Đã duyệt', statusClass: 'approved' },
  ];

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
