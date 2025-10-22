package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"sync/atomic"
)

var isReady int32 = 1

func main() {
	enableFeatureX := os.Getenv("ENABLE_FEATURE_X") == "true"

	log.Printf("Starting booking-service (Feature X: %v)", enableFeatureX)

	// Info endpoint
	http.HandleFunc("/info", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(map[string]interface{}{
			"featureX":  enableFeatureX,
			"service":   "booking-service-task5",
		})
	})

	// Health check endpoint (liveness probe)
	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(map[string]string{
			"status": "UP",
		})
	})

	// Readiness endpoint (readiness probe)
	http.HandleFunc("/ready", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		if atomic.LoadInt32(&isReady) == 1 {
			w.WriteHeader(http.StatusOK)
			json.NewEncoder(w).Encode(map[string]string{
				"status": "READY",
			})
		} else {
			w.WriteHeader(http.StatusServiceUnavailable)
			json.NewEncoder(w).Encode(map[string]string{
				"status": "NOT_READY",
			})
		}
	})

	http.HandleFunc("/ping", func(w http.ResponseWriter, r *http.Request) {
		if enableFeatureX {
			fmt.Fprintf(w, "pong from v2 (with Feature X)")
		} else {
			fmt.Fprintf(w, "pong from v1")
		}
	})

	// Feature flag route
	// if ENABLE_FEATURE_X=true, expose /feature
	if enableFeatureX {
		http.HandleFunc("/feature", func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"message": "Feature X is enabled!",
				"feature": "beta-feature",
			})
		})
	}

	// Booking endpoint to simulate business logic
	http.HandleFunc("/bookings", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		response := map[string]interface{}{
			"bookings": []map[string]string{
				{"id": "1", "hotel": "Grand Hotel", "status": "confirmed"},
				{"id": "2", "hotel": "Beach Resort", "status": "pending"},
			},
		}
		if enableFeatureX {
			response["enhanced"] = true
			response["message"] = "Enhanced booking features available"
		}
		json.NewEncoder(w).Encode(response)
	})

	log.Println("Server running on :8080")
	log.Fatal(http.ListenAndServe(":8080", nil))
}
