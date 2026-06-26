# Talent Pool API

## Shared Types

```ts
interface ResponseCommon<T> {
  success: boolean
  statusCode?: number
  message?: string
  data: T
}

interface PaginatedData<T> {
  items: T[]
  meta: {
    total: number
    page: number
    limit: number
    totalPages?: number
  }
}

interface CvFile {
  id: number
  uploaded_by: number
  original_file_name: string
  file_url: string
  file_type: string
  file_size: number
  ai_document_id?: string
  created_at: string
  updated_at: string
}

interface CvItem {
  cv_infos_id: number
  cv_file_id: number
  full_name: string
  email: string
  phone: string
  date_of_birth: string
  address: string
  summary: string
  educations: {
    field: string
    degree: string
    university: string
    graduation_year: number
  }[]
  certifications: string[]
  status: string
  created_at: string
  updated_at: string
  cv_file?: CvFile
  skills?: string[]
  position: string
  total_experience_years?: number
  work_type?: string
  tag?: 'new' | 'in-progress' | 'rejected' | 'hired' | 'contacted'
  is_marked?: boolean   // true = CV đã được đánh dấu vào Talent Pool
  notes?: string
  scores: {
    offline_score?: number
    matching_score?: number
    final_score?: number
  }
  reasons?: {
    offline_reason?: string
    matching_reason?: string
    overall_conclusion?: string
  }
}
```

---

## Authentication

Tất cả endpoint đều yêu cầu header:

```http
Authorization: Bearer <accessToken>
```

---

# 1. Get Talent Pool List

## POST /api/v1/cvs/talent-pool

Lấy danh sách các CV đã được đánh dấu vào Talent Pool (`is_marked = true`), có phân trang.

### Request Headers

```http
Authorization: Bearer <accessToken>
Content-Type: application/json
```

### Request Body

```json
{
  "page": 1,
  "limit": 10
}
```

| Field   | Type   | Required | Description                         |
|---------|--------|----------|-------------------------------------|
| `page`  | number | No       | Số trang, mặc định `1`              |
| `limit` | number | No       | Số bản ghi mỗi trang, mặc định `10` |

### Response — Thành công `200`

```json
{
  "success": true,
  "statusCode": 200,
  "message": "OK",
  "data": {
    "items": [
      {
        "cv_infos_id": 12,
        "cv_file_id": 8,
        "full_name": "Nguyen Van A",
        "email": "vana@example.com",
        "phone": "0901234567",
        "date_of_birth": "2001-05-10",
        "address": "Ha Noi",
        "summary": "Fresh graduate with strong ReactJS skills",
        "position": "Frontend Developer",
        "total_experience_years": 1,
        "work_type": "Full-time",
        "skills": ["ReactJS", "TypeScript", "TailwindCSS"],
        "educations": [
          {
            "field": "Computer Science",
            "degree": "Bachelor",
            "university": "HUST",
            "graduation_year": 2024
          }
        ],
        "certifications": ["AWS Cloud Practitioner"],
        "tag": "new",
        "is_marked": true,
        "notes": "Ứng viên tiềm năng, đã qua vòng phỏng vấn kỹ thuật",
        "status": "active",
        "created_at": "2026-06-01T08:00:00.000Z",
        "updated_at": "2026-06-20T14:30:00.000Z",
        "cv_file": {
          "id": 8,
          "uploaded_by": 3,
          "original_file_name": "NguyenVanA_CV.pdf",
          "file_url": "https://storage.example.com/cvs/NguyenVanA_CV.pdf",
          "file_type": "application/pdf",
          "file_size": 204800,
          "created_at": "2026-06-01T08:00:00.000Z",
          "updated_at": "2026-06-01T08:00:00.000Z"
        },
        "scores": {
          "offline_score": 87,
          "matching_score": 91,
          "final_score": 89
        },
        "reasons": {
          "offline_reason": "Strong technical background",
          "matching_reason": "Skills match 91% JD requirements",
          "overall_conclusion": "Highly recommended for next round"
        }
      }
    ],
    "meta": {
      "total": 24,
      "page": 1,
      "limit": 10,
      "totalPages": 3
    }
  }
}
```

### Response — Không có CV nào trong Talent Pool `200`

```json
{
  "success": true,
  "statusCode": 200,
  "message": "OK",
  "data": {
    "items": [],
    "meta": {
      "total": 0,
      "page": 1,
      "limit": 10,
      "totalPages": 0
    }
  }
}
```

### Response — Không có quyền truy cập `401`

```json
{
  "success": false,
  "statusCode": 401,
  "message": "Unauthorized",
  "data": null
}
```

---

# 2. Mark / Unmark CV as Talent

## PATCH /api/v1/cvs/:id/talent

Đánh dấu hoặc bỏ đánh dấu một CV trong Talent Pool thông qua field `is_marked`.

- `is_marked: true` → Thêm CV vào Talent Pool
- `is_marked: false` → Xóa CV khỏi Talent Pool

### Request Headers

```http
Authorization: Bearer <accessToken>
Content-Type: application/json
```

### Path Parameters

| Parameter | Type   | Required | Description          |
|-----------|--------|----------|----------------------|
| `id`      | number | Yes      | `cv_infos_id` của CV |

### Request Body

```json
{
  "is_marked": true
}
```

| Field       | Type    | Required | Description                                          |
|-------------|---------|----------|------------------------------------------------------|
| `is_marked` | boolean | Yes      | `true` để thêm vào Talent Pool, `false` để xóa khỏi |

### Response — Thành công (mark) `200`

Trả về toàn bộ CV object sau khi cập nhật:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "CV marked as talent successfully",
  "data": {
    "cv_infos_id": 12,
    "cv_file_id": 8,
    "full_name": "Nguyen Van A",
    "email": "vana@example.com",
    "phone": "0901234567",
    "date_of_birth": "2001-05-10",
    "address": "Ha Noi",
    "summary": "Fresh graduate with strong ReactJS skills",
    "position": "Frontend Developer",
    "total_experience_years": 1,
    "work_type": "Full-time",
    "skills": ["ReactJS", "TypeScript", "TailwindCSS"],
    "educations": [
      {
        "field": "Computer Science",
        "degree": "Bachelor",
        "university": "HUST",
        "graduation_year": 2024
      }
    ],
    "certifications": ["AWS Cloud Practitioner"],
    "tag": "new",
    "is_marked": true,
    "notes": "Ứng viên tiềm năng, đã qua vòng phỏng vấn kỹ thuật",
    "status": "active",
    "created_at": "2026-06-01T08:00:00.000Z",
    "updated_at": "2026-06-23T09:00:00.000Z",
    "cv_file": {
      "id": 8,
      "uploaded_by": 3,
      "original_file_name": "NguyenVanA_CV.pdf",
      "file_url": "https://storage.example.com/cvs/NguyenVanA_CV.pdf",
      "file_type": "application/pdf",
      "file_size": 204800,
      "created_at": "2026-06-01T08:00:00.000Z",
      "updated_at": "2026-06-01T08:00:00.000Z"
    },
    "scores": {
      "offline_score": 87,
      "matching_score": 91,
      "final_score": 89
    }
  }
}
```

### Response — Thành công (unmark) `200`

```json
{
  "success": true,
  "statusCode": 200,
  "message": "CV removed from talent pool",
  "data": {
    "cv_infos_id": 12,
    "is_marked": false,
    "updated_at": "2026-06-23T09:05:00.000Z"
  }
}
```

### Response — CV không tồn tại `404`

```json
{
  "success": false,
  "statusCode": 404,
  "message": "CV not found",
  "data": null
}
```

### Response — Không có quyền truy cập `401`

```json
{
  "success": false,
  "statusCode": 401,
  "message": "Unauthorized",
  "data": null
}
```

---

## Notes cho Backend

- Endpoint GET Talent Pool (`POST /api/v1/cvs/talent-pool`) chỉ trả về các CV có `is_marked = true`.
- Response của cả hai endpoint nên trả về toàn bộ object `CvItem` để frontend cập nhật state mà không cần gọi thêm.
